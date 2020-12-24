using System;
using System.Threading;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using ForceRenderer.IO;

namespace ForceRenderer.Renderers
{
	/// <summary>
	/// A worker class that process/render on a specific tile.
	/// Required resources should be provided to the worker first, then the worker is dispatched.
	/// After the work is done, the worker will wait for the resources and completed work to be swapped out and restart processing.
	/// </summary>
	public class TileWorker : IDisposable
	{
		public TileWorker(RenderEngine.Profile profile)
		{
			size = profile.tileSize;

			id = Interlocked.Increment(ref workerIdAccumulator);
			sampleCount = (long)size * size * profile.pixelSample;

			pixels = new Pixel[size * size];
			worker = new Thread(WorkThread)
					 {
						 IsBackground = true,
						 Name = $"Tile Worker #{id} {size}x{size}"
					 };

			for (int i = 0; i < pixels.Length; i++) pixels[i] = Pixel.Create();

			//Create golden ratio square spiral for pixel sample offsets
			pixelOffsets = new Float2[profile.pixelSample];

			for (int i = 0; i < profile.pixelSample; i++)
			{
				float theta = Scalars.TAU * Scalars.GoldenRatio * i;
				Float2 offset = new Float2(MathF.Cos(theta), MathF.Sin(theta));

				float square = 1f / (Math.Abs(MathF.Cos(theta + Scalars.PI / 4)) + Math.Abs(MathF.Sin(theta + Scalars.PI / 4)));
				float radius = MathF.Sqrt((i + 0.5f) / profile.pixelSample) * Scalars.Sqrt2 * square / 2f;

				pixelOffsets[i] = offset * radius + Float2.half;
			}
		}

		public readonly int id;
		public readonly int size;
		public readonly long sampleCount;

		volatile int _renderOffsetX;
		volatile int _renderOffsetY;

		volatile Texture _renderBuffer;
		volatile PixelWorker _pixelWorker;

		public Int2 RenderOffset => new Int2(_renderOffsetX, _renderOffsetY);

		public Texture RenderBuffer => _renderBuffer;
		public PixelWorker PixelWorker => _pixelWorker;

		long _completedSample;
		long _rejectedSample;

		public long CompletedSample => Interlocked.Read(ref _completedSample);
		public long RejectedSample => Interlocked.Read(ref _rejectedSample);

		readonly Pixel[] pixels;
		readonly Float2[] pixelOffsets; //Sample offset applied within each pixel
		readonly Thread worker;

		readonly ManualResetEventSlim resetEvent = new ManualResetEventSlim(); //Event sets when the worker is dispatched
		public bool Working => resetEvent.IsSet;

		static volatile int workerIdAccumulator;
		bool aborted;

		/// <summary>
		/// Invoked on worker thread when all rendered pixels are stored in the buffer.
		/// Passes in <see cref="TileWorker"/> for easy identification.
		/// </summary>
		public static event Action<TileWorker> OnWorkCompleted;

		public void ResetParameters(Int2 renderOffset, Texture renderBuffer = null, PixelWorker pixelWorker = null)
		{
			if (aborted) throw new Exception("Worker already aborted! It would not be used anymore!");
			if (Working) throw new Exception("Cannot reset when the worker is dispatched and already working!");

			_renderOffsetX = renderOffset.x;
			_renderOffsetY = renderOffset.y;

			_renderBuffer = renderBuffer ?? _renderBuffer;
			_pixelWorker = pixelWorker ?? _pixelWorker;

			Interlocked.Exchange(ref _completedSample, 0);
			Interlocked.Exchange(ref _rejectedSample, 0);
		}

		public void Dispatch()
		{
			if (aborted) throw new Exception("Worker already aborted!");
			if (Working) throw new Exception("Worker already dispatched!");

			if (PixelWorker == null) throw ExceptionHelper.Invalid(nameof(PixelWorker), InvalidType.isNull);
			if (RenderBuffer == null) throw ExceptionHelper.Invalid(nameof(RenderBuffer), InvalidType.isNull);

			if (!worker.IsAlive) worker.Start();
			resetEvent.Set();
		}

		void WorkThread()
		{
			while (!aborted)
			{
				resetEvent.Wait();
				if (aborted) break;

				try
				{
					if (!aborted) Parallel.For(0, sampleCount, WorkSample);   //Render samples
					if (!aborted) Parallel.For(0, pixels.Length, StorePixel); //Store pixels to buffer
				}
				finally { resetEvent.Reset(); }

				if (!aborted) OnWorkCompleted?.Invoke(this);
			}
		}

		void WorkSample(long sample, ParallelLoopState state)
		{
			if (aborted) state.Break();

			int pixelIndex = (int)(sample % pixels.Length);
			Int2 position = ToBufferPosition(pixelIndex);

			if (IsValid(position))
			{
				//Render pixel
				Float2 uv = ToAdjustedUV(position, sample);
				Float3 color = PixelWorker.Render(uv); //UV is adjusted to the correct scaling to match worker's requirement

				//Write to pixels
				pixels[pixelIndex].Accumulate(color);
			}
			else Interlocked.Increment(ref _rejectedSample);

			Interlocked.Increment(ref _completedSample);
		}

		void StorePixel(int pixelIndex, ParallelLoopState state)
		{
			if (aborted) state.Break();

			Int2 position = ToBufferPosition(pixelIndex);
			if (!IsValid(position)) return;

			ref Pixel pixel = ref pixels[pixelIndex];
			RenderBuffer.SetPixel(position, pixel.Color);
			pixel.Clear();
		}

		Int2 ToBufferPosition(int pixelIndex)
		{
			int x = pixelIndex % size + RenderOffset.x;
			int y = pixelIndex / size + RenderOffset.y;
			return new Int2(x, y);
		}

		Float2 ToAdjustedUV(Int2 bufferPosition, long sample)
		{
			Float2 offset = pixelOffsets[(int)(sample / pixels.Length)];
			Float2 uv = RenderBuffer.ToUV(bufferPosition + offset);
			return new Float2(uv.x - 0.5f, (uv.y - 0.5f) / RenderBuffer.aspect);
		}

		/// <summary>
		/// Check to make sure the <paramref name="bufferPosition"/> is inside our canvas; parts of some edge tiles might be outside.
		/// </summary>
		bool IsValid(Int2 bufferPosition) => bufferPosition.x >= 0 && bufferPosition.y >= 0 && bufferPosition.x < RenderBuffer.size.x && bufferPosition.y < RenderBuffer.size.y;

		public void Abort()
		{
			if (aborted) throw new Exception("Worker already aborted!");
			aborted = true;

			resetEvent.Set(); //Release the wait block
			worker.Join();
		}

		public void Dispose()
		{
			if (!aborted) Abort();
			resetEvent?.Dispose();
		}

		public override int GetHashCode() => id;
		public override string ToString() => $"Tile Worker #{id} {size}x{size}";

		struct Pixel
		{
			public static Pixel Create() => new Pixel(new object());
			Pixel(object locker) : this() => this.locker = locker;

			Double3 average;
			Double3 squared;

			int accumulation;
			readonly object locker;

			public const double MinDeviationThreshold = 0.3d;

			/// <summary>
			/// Returns the color average
			/// </summary>
			public Color32 Color
			{
				get
				{
					lock (locker) return (Color32)(Float3)average;
				}
			}

			/// <summary>
			/// Returns the standard deviation of the pixel
			/// </summary>
			public float Deviation
			{
				get
				{
					lock (locker)
					{
						double deviation = Math.Sqrt(squared.Max / accumulation);
						double max = Math.Max(average.Max, MinDeviationThreshold);

						return (float)(deviation / max);
					}
				}
			}

			/// <summary>
			/// Accumulates the color <paramref name="value"/> to pixel in a thread-safe manner.
			/// </summary>
			public void Accumulate(Float3 value)
			{
				if (float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z)) return; //NaN gate

				lock (locker)
				{
					accumulation++;

					Double3 oldMean = average;
					Double3 newValue = value;

					average += (newValue - oldMean) / accumulation;
					squared += (newValue - average) * (newValue - oldMean);
				}
			}

			public void Clear()
			{
				lock (locker)
				{
					average = default;
					squared = default;
					accumulation = 0;
				}
			}
		}

		readonly struct Double3
		{
			public Double3(double x, double y, double z)
			{
				this.x = x;
				this.y = y;
				this.z = z;
			}

			readonly double x;
			readonly double y;
			readonly double z;

			public double Max => Math.Max(x, Math.Max(y, z));

			public static Double3 operator +(Double3 first, Double3 second) => new Double3(first.x + second.x, first.y + second.y, first.z + second.z);
			public static Double3 operator -(Double3 first, Double3 second) => new Double3(first.x - second.x, first.y - second.y, first.z - second.z);

			public static Double3 operator *(Double3 first, Double3 second) => new Double3(first.x * second.x, first.y * second.y, first.z * second.z);
			public static Double3 operator /(Double3 first, Double3 second) => new Double3(first.x / second.x, first.y / second.y, first.z / second.z);

			public static Double3 operator *(Double3 first, double second) => new Double3(first.x * second, first.y * second, first.z * second);
			public static Double3 operator /(Double3 first, double second) => new Double3(first.x / second, first.y / second, first.z / second);

			public static Double3 operator *(double first, Double3 second) => new Double3(first * second.x, first * second.y, first * second.z);
			public static Double3 operator /(double first, Double3 second) => new Double3(first / second.x, first / second.y, first / second.z);

			public static implicit operator Double3(Float3 value) => new Double3(value.x, value.y, value.z);
			public static explicit operator Float3(Double3 value) => new Float3((float)value.x, (float)value.y, (float)value.z);
		}
	}
}