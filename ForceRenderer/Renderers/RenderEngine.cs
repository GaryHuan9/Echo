using System;
using System.Linq;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Vectors;
using ForceRenderer.IO;
using ForceRenderer.Objects;
using ForceRenderer.Scenes;

namespace ForceRenderer.Renderers
{
	public class RenderEngine : IDisposable
	{
		public RenderEngine() => TileWorker.OnWorkCompleted += OnTileWorkCompleted;

		public Scene Scene { get; set; }
		public int PixelSample { get; set; }

		public int TileSize { get; set; }
		public int WorkerSize { get; set; } = Environment.ProcessorCount;

		public int MaxBounce { get; set; } = 32;
		public float EnergyEpsilon { get; set; } = 1E-3f; //Epsilon lower bound value to determine when an energy is essentially zero

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
			get => (State)Interlocked.CompareExchange(ref _currentState, 0, 0);
			private set => Interlocked.Exchange(ref _currentState, (int)value);
		}

		public bool Completed => CurrentState == State.completed;

		Int2[] tilePositions;   //Positions of tiles. Processed from 0 to length. Positions can be in any order.
		int[] tileWorkerStatus; //Status of tiles, negative means either completed or not yet started. Positive indicates worker index

		volatile int dispatchedTileCount; //Number of tiles being processed or are already processed.
		TileWorker[] workers;             //All of the workers that should process the tiles

		public int DispatchedTileCount => Interlocked.CompareExchange(ref dispatchedTileCount, 0, 0);
		public int TotalTileCount => tilePositions?.Length ?? 0; //The number of processed tiles or tiles currently being processed

		readonly object manageLocker = new object(); //Locker used when managing any of the workers

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

			CurrentState = State.rendering;

			CreateTilePositions();
			InitializeWorkers();
		}

		void CreateTilePositions()
		{
			Int2.LoopEnumerable grid = RenderBuffer.size.CeiledDivide(profile.tileSize).Loop();

			tilePositions = grid.Select(position => position * profile.tileSize).ToArray();
			tileWorkerStatus = Enumerable.Repeat(-1, TotalTileCount).ToArray();

			tilePositions.Shuffle(); //Shuffle it just for fun
		}

		void InitializeWorkers()
		{
			lock (manageLocker)
			{
				PixelWorker pixelWorker = new PathTraceWorker(profile);

				for (int i = 0; i < profile.workerSize; i++)
				{
					TileWorker worker = new TileWorker(profile);

					worker.ResetParameters(Int2.zero, RenderBuffer, pixelWorker);
					DispatchWorker(worker);
				}
			}
		}

		void DispatchWorker(TileWorker worker)
		{
			lock (manageLocker)
			{
				int count = DispatchedTileCount;
				if (count == TotalTileCount) return;

				worker.ResetParameters(tilePositions[count]);
				worker.Dispatch();

				tileWorkerStatus[count] = Array.IndexOf(workers, worker);
				Interlocked.Increment(ref dispatchedTileCount);
			}
		}

		void OnTileWorkCompleted(TileWorker worker)
		{
			lock (manageLocker)
			{
				if (DispatchedTileCount == TotalTileCount) CurrentState = State.completed;
				else if (CurrentState == State.rendering) DispatchWorker(worker);
			}
		}

		/// <summary>
		/// Returns the <see cref="TileWorker"/> working on <paramref name="tile"/>. Returns null if no worker is currently working on it.
		/// <paramref name="completed"/> will be set to true if a worker already finished the tile. No worker will be returned if a tile is done.
		/// </summary>
		public TileWorker GetWorker(Int2 tile, out bool completed)
		{
			throw new NotImplementedException();
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
			if (workers != null)
			{
				for (int i = 0; i < workers.Length; i++) workers[i].Dispose();
			}

			workers = null;
			profile = default;

			tilePositions = null;
			tileWorkerStatus = null;
			dispatchedTileCount = 0;

			GC.Collect();
			CurrentState = State.waiting;
		}

		public void Dispose()
		{
			if (workers != null)
			{
				for (int i = 0; i < workers.Length; i++) workers[i].Dispose();
			}

			TileWorker.OnWorkCompleted -= OnTileWorkCompleted;
		}

		public enum State
		{
			waiting,
			rendering,
			completed,
			stopped
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