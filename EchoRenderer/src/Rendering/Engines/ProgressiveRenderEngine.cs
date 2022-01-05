using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CodeHelpers.Collections;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Pixels;
using EchoRenderer.Rendering.Profiles;
using EchoRenderer.Textures.Grid;
using ThreadState = System.Threading.ThreadState;

namespace EchoRenderer.Rendering.Engines
{
	public class ProgressiveRenderEngine : IDisposable
	{
		public ProgressiveRenderEngine()
		{
			workThread = new Thread(WorkThread);
			renderData = new RenderData();
		}

		public ProgressiveRenderProfile CurrentProfile { get; private set; }

		int _currentState;
		int _epoch;

		public State CurrentState
		{
			get => (State)InterlockedHelper.Read(ref _currentState);
			private set
			{
				int state = (int)value;

				if (Interlocked.Exchange(ref _currentState, state) == state) return;
				lock (signalLocker) Monitor.PulseAll(signalLocker);
			}
		}

		public int Epoch
		{
			get => InterlockedHelper.Read(ref _epoch);
			private set => Interlocked.Exchange(ref _epoch, value);
		}

		public TimeSpan Elapsed => stopwatch.Elapsed;
		bool Disposed => CurrentState == State.disposed;

		readonly Thread workThread;
		readonly RenderData renderData;

		readonly Stopwatch stopwatch = new();
		readonly object signalLocker = new();

		ThreadLocal<Arena> threadArena;
		ParallelOptions parallelOptions;

		public void Begin(ProgressiveRenderProfile profile)
		{
			ThrowIfDisposed();
			if (CurrentState != State.waiting) throw new Exception($"Must change to {nameof(State.waiting)} with {nameof(Stop)} before starting!");

			profile.Validate();
			CurrentProfile = profile;

			CurrentState = State.initialization;

			Epoch = 0;
			threadArena = new ThreadLocal<Arena>(CreateArena);

			profile.Method.Prepare(profile);
			profile.Scene.ResetIntersectionCount();

			renderData.Recreate(profile.RenderBuffer);

			parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = profile.WorkerSize };
			if (workThread.ThreadState == ThreadState.Unstarted) workThread.Start();

			stopwatch.Restart();
			CurrentState = State.rendering;
		}

		public void Stop()
		{
			ThrowIfDisposed();
			stopwatch.Stop();

			CurrentState = State.waiting;
			CurrentProfile = null;
		}

		public void Dispose()
		{
			if (Disposed) return;
			CurrentState = State.disposed;

			if (workThread.IsAlive) workThread.Join();
		}

		public void WaitForState(State state)
		{
			while (true)
			{
				State current = CurrentState;
				if (current == state || Disposed) return;

				lock (signalLocker) Monitor.Wait(signalLocker);
			}
		}

		void WorkThread()
		{
			while (!Disposed)
			{
				WaitForState(State.rendering);

				++Epoch;

				Parallel.For(0, renderData.Size, parallelOptions, WorkPixel);
			}
		}

		void WorkPixel(int index, ParallelLoopState state)
		{
			var profile = CurrentProfile;
			if (profile == null) return;

			Int2 position = renderData.GetPosition(index);

			if (CurrentState != State.rendering) state.Break();
			ref RenderPixel pixel = ref renderData[position];

			var buffer = profile.RenderBuffer;
			var method = profile.Method;

			Arena arena = threadArena.Value;
			double sampleCount = profile.EpochSample;

			//TODO: switch from progressive epoch sampling to regular pixel sampling after scene stopped changing
			throw new NotImplementedException();

			if (Epoch > profile.EpochLength)
			{
				sampleCount += profile.AdaptiveSample;
				sampleCount *= pixel.Deviation;
			}

			for (int i = 0; i < sampleCount; i++)
			{
				//Sample color
				Float2 uv = (position + arena.Random.NextSample()) / buffer.size - Float2.half;
				var sample = method.Render(uv.ReplaceY(uv.y / buffer.aspect), arena);

				bool successful = pixel.Accumulate(sample);
			}

			pixel.Store(renderData.Buffer, position);
		}

		void ThrowIfDisposed()
		{
			if (!Disposed) return;
			throw new Exception($"Operation invalid after {nameof(Dispose)}!");
		}

		/// <summary>
		/// Creates a new <see cref="Arena"/> for the invocation thread and <see cref="CurrentProfile"/>.
		/// </summary>
		Arena CreateArena()
		{
			int id = Thread.CurrentThread.ManagedThreadId;
			long tick = Environment.TickCount64;

			RenderProfile profile = CurrentProfile;
			uint seed = (uint)HashCode.Combine(id, tick);
			return profile.Method.CreateArena(profile, seed);
		}

		public enum State
		{
			waiting,
			initialization,
			rendering,
			disposed
		}

		class RenderData
		{
			public RenderBuffer Buffer { get; private set; }
			public int Size => Buffer.size.Product;

			RenderPixel[] pixels;

			int[] patternMajor;
			int[] patternMinor;

			public ref RenderPixel this[Int2 position] => ref pixels[Buffer.ToIndex(position)];

			public void Recreate(RenderBuffer newBuffer)
			{
				RebuildMajor(ref patternMajor, newBuffer.size[ArrayGrid.MajorAxis]);
				RebuildPattern(ref patternMinor, newBuffer.size[ArrayGrid.MinorAxis]);

				int oldLength = Buffer?.size.Product ?? 0;
				int newLength = newBuffer.size.Product;

				if (oldLength >= newLength) Array.Clear(pixels, 0, newLength);
				else pixels = new RenderPixel[newLength];

				if (newBuffer is ProgressiveRenderBuffer buffer) buffer.ClearWrittenFlagArray();

				Buffer = newBuffer;
			}

			public Int2 GetPosition(int index) => Int2.Create
			(
				ArrayGrid.MajorAxis,
				patternMajor[index / patternMinor.Length],
				patternMinor[index % patternMinor.Length]
			);

			static void RebuildMajor(ref int[] pattern, int size)
			{
				RebuildPattern(ref pattern, size);
				pattern.Swap(0, pattern.IndexOf(0));
			}

			static void RebuildPattern(ref int[] pattern, int size)
			{
				if (pattern?.Length != size) pattern = new int[size];
				for (int i = 0; i < pattern.Length; i++) pattern[i] = i;

				pattern.Shuffle();
			}
		}
	}
}