using System;
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
			renderData.Recreate(profile.RenderBuffer);

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

			ExtendedRandom random = threadRandom.Value;

			for (int i = 0; i < profile.EpochSample; i++)
			{
				//Sample color
				Float2 uv = (position + random.NextSample()) / buffer.size - Float2.half;
				var sample = method.Render(uv.ReplaceY(uv.y / buffer.aspect), random);

				bool successful = pixel.Accumulate(sample);
			}

			pixel.Store(renderData.Buffer, position);
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
			public RenderBuffer Buffer { get; private set; }
			public int Size => Buffer.size.Product;

			RenderPixel[] pixels;

			int[] patternMajor;
			int[] patternMinor;

			public ref RenderPixel this[Int2 position] => ref pixels[Buffer.ToIndex(position)];

			public void Recreate(RenderBuffer newBuffer)
			{
				RebuildMajor(ref patternMajor, newBuffer.size[Array2D.MajorAxis]);
				RebuildPattern(ref patternMinor, newBuffer.size[Array2D.MinorAxis]);

				int oldLength = Buffer?.size.Product ?? 0;
				int newLength = newBuffer.size.Product;

				if (oldLength >= newLength) Array.Clear(pixels, 0, newLength);
				else pixels = new RenderPixel[newLength];

				Buffer = newBuffer;
				Buffer.Clear();
			}

			public Int2 GetPosition(int index) => Int2.Create
			(
				Array2D.MajorAxis,
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