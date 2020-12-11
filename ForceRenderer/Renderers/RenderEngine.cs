using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Threads;
using CodeHelpers.Vectors;
using ForceRenderer.IO;
using ForceRenderer.Objects;

namespace ForceRenderer.Renderers
{
	public class RenderEngine : IDisposable
	{
		public RenderEngine() => TileWorker.OnWorkCompleted += OnTileWorkCompleted;

		public Scene Scene { get; set; }
		public int PixelSample { get; set; }

		public int TileSize { get; set; }
		public int WorkerSize { get; set; } = Environment.ProcessorCount / 2;

		public int MaxBounce { get; set; } = 32;
		public float EnergyEpsilon { get; set; } = 1E-2f; //Epsilon lower bound value to determine when an energy is essentially zero

		Profile profile;

		volatile Texture _renderBuffer;
		volatile int _currentState;

		public Texture RenderBuffer
		{
			get => _renderBuffer;
			set
			{
				if (CurrentState == State.rendering) throw new Exception("Cannot modify buffer when rendering!");
				Interlocked.Exchange(ref _renderBuffer, value);
			}
		}

		public State CurrentState
		{
			get => (State)InterlockedHelper.Read(ref _currentState);
			private set => Interlocked.Exchange(ref _currentState, (int)value);
		}

		public bool Completed => CurrentState == State.completed;
		public Profile CurrentProfile => profile;

		public PixelWorker PixelWorker { get; private set; }
		public TimeSpan Elapsed => stopwatch.Elapsed;

		TileWorker[] workers; //All of the workers that should process the tiles
		Int2[] tilePositions; //Positions of tiles. Processed from 0 to length. Positions can be in any order.

		Dictionary<Int2, TileStatus> tileStatuses; //Indexer to status of tiles, tile position in tile-space, meaning the gap between tiles is one
		Stopwatch stopwatch;

		volatile int dispatchedTileCount; //Number of tiles being processed or are already processed.
		volatile int completedTileCount;  //Number of tiles finished processing

		public int DispatchedTileCount => Interlocked.CompareExchange(ref dispatchedTileCount, 0, 0);
		public int CompletedTileCount => Interlocked.CompareExchange(ref completedTileCount, 0, 0);

		public long InitiatedSample
		{
			get
			{
				lock (manageLocker)
				{
					long completedSample = (long)CompletedTileCount * profile.tileSize * profile.tileSize * profile.pixelSample;
					return completedSample + workers.Where(worker => worker.Working).Sum(worker => worker.InitiatedSample);
				}
			}
		}

		public long CompletedSample
		{
			get
			{
				lock (manageLocker)
				{
					long completedSample = (long)CompletedTileCount * profile.tileSize * profile.tileSize * profile.pixelSample;
					return completedSample + workers.Where(worker => worker.Working).Sum(worker => worker.CompletedSample);
				}
			}
		}

		public Int2 TotalTileSize { get; private set; }     //The size of the rendering tile grid
		public int TotalTileCount => TotalTileSize.Product; //The number of processed tiles or tiles currently being processed

		readonly object manageLocker = new object(); //Locker used when managing any of the workers
		readonly ManualResetEvent renderCompleteEvent = new ManualResetEvent(false);

		public void Begin()
		{
			if (RenderBuffer == null) throw ExceptionHelper.Invalid(nameof(RenderBuffer), this, InvalidType.isNull);
			if (CurrentState != State.waiting) throw new Exception("Incorrect state! Must reset before rendering!");

			PressedScene pressed = new PressedScene(Scene);
			profile = new Profile(pressed, this);

			if (pressed.camera == null) throw new Exception("No camera in scene! Cannot render without a camera!");
			if (PixelSample <= 0) throw ExceptionHelper.Invalid(nameof(PixelSample), PixelSample, "must be positive!");
			if (TileSize <= 0) throw ExceptionHelper.Invalid(nameof(TileSize), TileSize, "must be positive!");
			if (MaxBounce < 0) throw ExceptionHelper.Invalid(nameof(MaxBounce), MaxBounce, "cannot be negative!");
			if (EnergyEpsilon < 0f) throw ExceptionHelper.Invalid(nameof(EnergyEpsilon), EnergyEpsilon, "cannot be negative!");

			CurrentState = State.initializing;

			CreateTilePositions();
			InitializeWorkers();

			stopwatch = Stopwatch.StartNew();
			CurrentState = State.rendering;
		}

		void CreateTilePositions()
		{
			TotalTileSize = RenderBuffer.size.CeiledDivide(profile.tileSize);
			tilePositions = TotalTileSize.Loop().Select(position => position * profile.tileSize).ToArray();

			//Shuffle it just for fun, we might reposition them differently (spiral or checkerboard .etc) later.
			tilePositions.Shuffle();

			tileStatuses = tilePositions.ToDictionary
			(
				position => position / profile.tileSize,
				position => new TileStatus(position)
			);
		}

		void InitializeWorkers()
		{
			PixelWorker = new PathTraceWorker(profile);
			workers = new TileWorker[profile.workerSize];

			for (int i = 0; i < profile.workerSize; i++)
			{
				TileWorker worker = workers[i] = new TileWorker(profile);

				worker.ResetParameters(Int2.zero, RenderBuffer, PixelWorker);
				DispatchWorker(worker);
			}
		}

		void DispatchWorker(TileWorker worker)
		{
			lock (manageLocker)
			{
				int count = DispatchedTileCount;
				if (count == TotalTileCount) return;

				Int2 renderPosition = tilePositions[count];
				Int2 statusPosition = renderPosition / profile.tileSize;
				TileStatus status = tileStatuses[statusPosition];

				worker.ResetParameters(renderPosition);
				worker.Dispatch();

				Interlocked.Increment(ref dispatchedTileCount);
				tileStatuses[statusPosition] = new TileStatus(status, Array.IndexOf(workers, worker));
			}
		}

		void OnTileWorkCompleted(TileWorker worker)
		{
			lock (manageLocker)
			{
				Interlocked.Increment(ref completedTileCount);

				if (CompletedTileCount == TotalTileCount)
				{
					CurrentState = State.completed;
					stopwatch.Stop();
					renderCompleteEvent.Set();
				}
				else if (CurrentState == State.rendering) DispatchWorker(worker);
			}
		}

		/// <summary>
		/// Returns the <see cref="TileWorker"/> working on or worked on <paramref name="tile"/>.
		/// <paramref name="completed"/> will be set to true if a worker already finished the tile.
		/// <paramref name="tile"/> should be the position of indicating tile in tile-space, where the gap between tiles is one.
		/// </summary>
		public TileWorker GetWorker(Int2 tile, out bool completed)
		{
			lock (manageLocker)
			{
				if (!tileStatuses.ContainsKey(tile)) throw ExceptionHelper.Invalid(nameof(tile), tile, InvalidType.outOfBounds);

				TileStatus status = tileStatuses[tile];

				if (status.worker < 0)
				{
					completed = false;
					return null;
				}

				TileWorker worker = workers[status.worker];
				completed = !worker.Working || status.position != worker.RenderOffset;

				return worker;
			}
		}

		/// <summary>
		/// Aborts current render session.
		/// </summary>
		public void Abort()
		{
			//TODO Stop the workers
			CurrentState = State.stopped;
		}

		/// <summary>
		/// Resets engine for another rendering.
		/// </summary>
		public void Reset()
		{
			lock (manageLocker)
			{
				if (workers != null)
				{
					for (int i = 0; i < workers.Length; i++) workers[i].Dispose();
				}

				workers = null;
				renderCompleteEvent.Reset();

				tilePositions = null;
				tileStatuses = null;
			}

			dispatchedTileCount = 0;
			completedTileCount = 0;

			profile = default;
			TotalTileSize = default;

			PixelWorker = null;
			stopwatch = null;

			GC.Collect();
			CurrentState = State.waiting;
		}

		public void WaitForRender()
		{
			renderCompleteEvent.WaitOne(); //If multiple threads are waiting, the event
			renderCompleteEvent.Reset();   //will get reset multiple times but it's fine
		}

		public void Dispose()
		{
			if (workers != null)
			{
				for (int i = 0; i < workers.Length; i++) workers[i].Dispose();
			}

			TileWorker.OnWorkCompleted -= OnTileWorkCompleted;
			renderCompleteEvent.Dispose();
		}

		public enum State
		{
			waiting,
			initializing,
			rendering,
			completed,
			stopped
		}

		readonly struct TileStatus
		{
			public TileStatus(Int2 position, int worker = -1)
			{
				this.position = position;
				this.worker = worker;
			}

			public TileStatus(TileStatus status, int worker) : this(status.position, worker) { }

			public readonly Int2 position;
			public readonly int worker;
		}

		/// <summary>
		/// An immutable structure that is stores a copy of the renderer's settings/profile.
		/// This ensures that the renderer never changes its settings when all threads are running.
		/// </summary>
		public readonly struct Profile
		{
			public Profile(PressedScene pressed, RenderEngine engine)
			{
				this.pressed = pressed;
				scene = pressed.source;
				camera = pressed.camera;

				pixelSample = engine.PixelSample;
				tileSize = engine.TileSize;
				workerSize = engine.WorkerSize;

				maxBounce = engine.MaxBounce;
				energyEpsilon = engine.EnergyEpsilon;
			}

			public readonly PressedScene pressed;
			public readonly Scene scene;
			public readonly Camera camera;

			public readonly int pixelSample;
			public readonly int tileSize;
			public readonly int workerSize;

			public readonly int maxBounce;
			public readonly float energyEpsilon;
		}
	}
}