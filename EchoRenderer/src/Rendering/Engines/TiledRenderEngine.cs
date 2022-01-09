using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;

namespace EchoRenderer.Rendering.Engines
{
	public class TiledRenderEngine : IDisposable
	{
		int _currentState;

		public State CurrentState
		{
			get => (State)InterlockedHelper.Read(ref _currentState);
			private set
			{
				if (Interlocked.Exchange(ref _currentState, (int)value) == (int)value) return;
				lock (signalLocker) Monitor.PulseAll(signalLocker);
			}
		}

		public bool Completed => CurrentState == State.completed;
		public bool Rendering => CurrentState == State.rendering || CurrentState == State.paused;

		public TimeSpan Elapsed => stopwatch.Elapsed;
		public TiledRenderProfile CurrentProfile { get; private set; }

		TileWorker[] workers; //All of the workers that should process the tiles
		Int2[] tilePositions; //Positions of tiles. Processed from 0 to length. Positions can be in any order.

		Dictionary<Int2, TileStatus> tileStatuses; //Indexer to status of tiles, tile position in tile-space, meaning the gap between tiles is one
		readonly Stopwatch stopwatch = new();

		volatile int dispatchedTileCount; //Number of tiles being processed or are already processed.
		volatile int completedTileCount;  //Number of tiles finished processing

		public int DispatchedTileCount => Interlocked.CompareExchange(ref dispatchedTileCount, 0, 0);
		public int CompletedTileCount => Interlocked.CompareExchange(ref completedTileCount, 0, 0);

		long fullyCompletedSample; //Samples that are rendered in completed tiles
		long fullyCompletedPixel;  //Pixels that are rendered in completed tiles
		long fullyRejectedSample;  //Samples that are rejected in completed tiles

		public long CompletedSample
		{
			get
			{
				lock (manageLocker) return fullyCompletedSample + workers.Where(worker => worker.Working).Sum(worker => worker.CompletedSample);
			}
		}

		public long CompletedPixel
		{
			get
			{
				lock (manageLocker) return fullyCompletedPixel + workers.Where(worker => worker.Working).Sum(worker => worker.CompletedPixel);
			}
		}

		public long RejectedSample
		{
			get
			{
				lock (manageLocker) return fullyRejectedSample + workers.Where(worker => worker.Working).Sum(worker => worker.RejectedSample);
			}
		}

		public Int2 TotalTileSize { get; private set; }     //The size of the rendering tile grid
		public int TotalTileCount => TotalTileSize.Product; //The number of processed tiles or tiles currently being processed

		readonly object manageLocker = new(); //Locker used when managing any of the workers
		readonly object signalLocker = new(); //Locker to signal when state changed

		public void Begin(TiledRenderProfile profile)
		{
			if (CurrentState != State.waiting) throw new Exception("Incorrect state! Must reset before rendering!");

			profile.Validate();
			CurrentProfile = profile;

			profile.Scene.ResetIntersectionCount();
			profile.Method.Prepare(profile);

			lock (manageLocker)
			{
				CurrentState = State.initialization;

				CreateTilePositions();
				InitializeWorkers();

				stopwatch.Restart();
				CurrentState = State.rendering;
			}
		}

		void CreateTilePositions()
		{
			TotalTileSize = CurrentProfile.RenderBuffer.size.CeiledDivide(CurrentProfile.TileSize);
			tilePositions = CurrentProfile.TilePattern.GetPattern(TotalTileSize);

			for (int i = 0; i < tilePositions.Length; i++) tilePositions[i] *= CurrentProfile.TileSize;

			tileStatuses = tilePositions.ToDictionary
			(
				position => position / CurrentProfile.TileSize,
				position => new TileStatus(position)
			);
		}

		void InitializeWorkers()
		{
			workers = new TileWorker[CurrentProfile.WorkerSize];

			for (int i = 0; i < workers.Length; i++)
			{
				TileWorker worker = workers[i] = new TileWorker(CurrentProfile);

				worker.OnWorkCompletedMethods += OnTileWorkCompleted;
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
				Int2 statusPosition = renderPosition / CurrentProfile.TileSize;
				TileStatus status = tileStatuses[statusPosition];

				worker.Reset(renderPosition);
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

				Interlocked.Add(ref fullyCompletedSample, worker.CompletedSample);
				Interlocked.Add(ref fullyCompletedPixel, worker.CompletedPixel);
				Interlocked.Add(ref fullyRejectedSample, worker.RejectedSample);

				if (CompletedTileCount == TotalTileCount)
				{
					stopwatch.Stop();
					CurrentState = State.completed;

					return;
				}

				switch (CurrentState)
				{
					case State.rendering:
					{
						DispatchWorker(worker);
						break;
					}
					case State.paused:
					{
						if (CompletedTileCount == DispatchedTileCount) stopwatch.Stop();
						break;
					}
					default: throw ExceptionHelper.Invalid(nameof(CurrentState), CurrentState, InvalidType.unexpected);
				}
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
		/// Pause the current render session.
		/// </summary>
		public void Pause()
		{
			if (CurrentState == State.rendering) CurrentState = State.paused; //Timer will be paused when all worker finish
			else throw new Exception("Not rendering! No render session to pause.");
		}

		/// <summary>
		/// Resume the current render session.
		/// </summary>
		public void Resume()
		{
			if (CurrentState != State.paused) throw new Exception("Not paused! No render session to resume.");

			lock (manageLocker)
			{
				for (int i = 0; i < CurrentProfile.WorkerSize; i++)
				{
					TileWorker worker = workers[i];
					if (worker.Working) continue;

					DispatchWorker(worker);
				}
			}

			stopwatch.Start();
			CurrentState = State.rendering;
		}

		/// <summary>
		/// Aborts current render session.
		/// </summary>
		public void Abort()
		{
			if (!Rendering) throw new Exception("Not rendering! No render session to abort.");
			for (int i = 0; i < workers.Length; i++) workers[i].Abort();

			stopwatch.Stop();
			CurrentState = State.aborted;
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

				tilePositions = null;
				tileStatuses = null;
			}

			dispatchedTileCount = 0;
			completedTileCount = 0;

			fullyCompletedSample = 0;
			fullyCompletedPixel = 0;

			CurrentProfile = default;
			TotalTileSize = default;

			stopwatch.Reset();

			GC.Collect();
			CurrentState = State.waiting;
		}

		public void WaitForRender()
		{
			while (true)
			{
				State current = CurrentState;

				if (current == State.aborted) return;
				if (current == State.completed) return;

				lock (signalLocker) Monitor.Wait(signalLocker);
			}
		}

		public void Dispose()
		{
			if (workers == null) return;

			foreach (var worker in workers)
			{
				worker.OnWorkCompletedMethods -= OnTileWorkCompleted;
				worker.Dispose();
			}

			workers = null;
		}

		public enum State
		{
			waiting,
			initialization,
			rendering,
			paused,
			completed,
			aborted
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
	}
}