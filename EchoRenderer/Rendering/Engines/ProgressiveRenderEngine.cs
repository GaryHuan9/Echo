using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeHelpers.Collections;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using EchoRenderer.Mathematics;
using EchoRenderer.Rendering.Pixels;
using EchoRenderer.Textures;

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

		bool Disposed => CurrentState == State.disposed;

		readonly Thread workThread;
		readonly RenderData renderData;

		ParallelOptions parallelOptions;
		readonly object signalLocker = new object();

		readonly ThreadLocal<ExtendedRandom> threadRandom = new(() => new ExtendedRandom());

		public void Begin(ProgressiveRenderProfile profile)
		{
			ThrowIfDisposed();
			if (CurrentState != State.waiting) throw new Exception($"Must change to {nameof(State.waiting)} with {nameof(Stop)} before starting!");

			profile.Validate();
			CurrentProfile = profile;

			CurrentState = State.initialization;

			profile.Method.AssignProfile(profile);
			renderData.Clean(profile.RenderBuffer);

			parallelOptions = new ParallelOptions {MaxDegreeOfParallelism = profile.WorkerSize};
			if (workThread.ThreadState == ThreadState.Unstarted) workThread.Start();

			CurrentState = State.rendering;
		}

		public void Stop()
		{
			ThrowIfDisposed();

			CurrentState = State.waiting;
			CurrentProfile = null;
		}

		public void Dispose()
		{
			if (Disposed) return;

			CurrentState = State.disposed;
			workThread.Join();
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
				Parallel.For(0, renderData.Length, parallelOptions, WorkPixel);
			}
		}

		void WorkPixel(int index, ParallelLoopState state)
		{
			var profile = CurrentProfile;
			if (profile == null) return;

			Int2 position = renderData[index];

			if (CurrentState != State.rendering) state.Break();
			ref RenderPixel pixel = ref renderData[position];

			var buffer = profile.RenderBuffer;
			var method = profile.Method;

			ExtendedRandom random = threadRandom.Value;

			for (int i = 0; i < profile.EpochSample; i++)
			{
				//Sample color
				Float2 uv = (position + random.NextSample()) / buffer.size - Float2.half;
				PixelWorker.Sample sample = method.Render(uv.ReplaceY(uv.y / buffer.aspect));

				bool successful = pixel.Accumulate(sample);
			}

			renderData.Store(position);
		}

		void ThrowIfDisposed()
		{
			if (!Disposed) return;
			throw new Exception($"Operation invalid after {nameof(Dispose)}!");
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
			RenderBuffer buffer;
			RenderPixel[] pixels;

			Int2[] pattern;

			public int Length => pattern.Length;
			public Int2 this[int index] => pattern[index];

			public ref RenderPixel this[Int2 position] => ref pixels[buffer.ToIndex(position)];

			public void Clean(RenderBuffer newBuffer)
			{
				Int2 oldSize = buffer?.size ?? Int2.zero;
				Int2 newSize = newBuffer.size;

				if (oldSize != newSize)
				{
					pixels = new RenderPixel[newSize.Product];
					pattern = newSize.Loop().ToArray();

					pattern.Shuffle();
				}
				else Array.Clear(pixels, 0, pixels.Length);

				buffer = newBuffer;
				buffer.Clear();
			}

			public void Store(Int2 position)
			{
				ref var pixel = ref this[position];
				pixel.Store(buffer, position);
			}
		}
	}
}