using System;
using System.Threading;
using CodeHelpers;
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

		volatile int dispatchedTileCount;

		public int DispatchedTileCount => Interlocked.CompareExchange(ref dispatchedTileCount, 0, 0);
		public int TotalTileCount => tilePattern?.Length ?? 0; //The number of processed tiles or tiles currently being processed

		TileWorker[] workers;
		TilePattern tilePattern;

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
			InitializeWorkers();
		}

		void InitializeWorkers()
		{
			tilePattern = new TilePattern(RenderBuffer.size, profile.tileSize);

			SamplePattern samplePattern = new SamplePattern(profile.pixelSample);
			PixelWorker pixelWorker = new PathTraceWorker(profile);

			for (int i = 0; i < profile.workerSize; i++)
			{
				TileWorker worker = new TileWorker(profile);

				worker.ResetParameters(Int2.zero, RenderBuffer, pixelWorker, samplePattern);
				DispatchWorker(worker);
			}
		}

		void DispatchWorker(TileWorker worker)
		{
			lock (manageLocker)
			{
				int count = DispatchedTileCount;
				if (count == tilePattern.Length) return;

				worker.ResetParameters(tilePattern[count]);
				worker.Dispatch();

				Interlocked.Increment(ref dispatchedTileCount);
			}
		}

		void OnTileWorkCompleted(TileWorker worker)
		{
			lock (manageLocker)
			{
				if (DispatchedTileCount == tilePattern.Length) CurrentState = State.completed;
				else if (CurrentState == State.rendering) DispatchWorker(worker);
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
			if (workers != null)
			{
				for (int i = 0; i < workers.Length; i++) workers[i].Dispose();
			}

			workers = null;
			profile = default;

			tilePattern = null;
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