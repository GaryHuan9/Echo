using System;
using System.Threading;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Vectors;
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
			this.profile = profile;
			size = profile.tileSize;

			id = Interlocked.Increment(ref workerIdAccumulator);
			sampleCount = (long)size * size * profile.pixelSample;

			pixels = new Pixel[size * size];
			worker = new Thread(WorkThread)
					 {
						 IsBackground = true,
						 // Priority = ThreadPriority.Highest,
						 Name = $"Tile Worker {size}x{size} #{id}"
					 };
		}

		public readonly int id;
		public readonly RenderEngine.Profile profile;

		public readonly int size;
		public readonly long sampleCount;

		public Int2 RenderOffset { get; private set; }
		public Texture RenderBuffer { get; private set; }

		public PixelWorker PixelWorker { get; private set; }
		public SamplePattern SamplePattern { get; private set; }

		long _completedSample;
		long _initiatedSample;

		public long CompletedSample => Interlocked.Read(ref _completedSample);
		public long InitiatedSample => Interlocked.Read(ref _initiatedSample);

		readonly Pixel[] pixels;
		readonly Thread worker;

		readonly ManualResetEventSlim resetEvent = new ManualResetEventSlim(); //Event sets when the worker is dispatched
		readonly SemaphoreSlim writeSemaphore = new SemaphoreSlim(1, 1);

		public bool Working => resetEvent.IsSet;

		bool disposed;
		static volatile int workerIdAccumulator;

		/// <summary>
		/// Invoked on worker thread when all rendered pixels are stored in the buffer.
		/// Passes in <see cref="TileWorker"/> for easy identification.
		/// </summary>
		public static event Action<TileWorker> OnWorkCompleted;

		public void ResetParameters(Int2 renderOffset, Texture renderBuffer = null, PixelWorker pixelWorker = null, SamplePattern samplePattern = null)
		{
			if (Working) throw new Exception("Cannot reset when the worker is dispatched and already working!");

			RenderOffset = renderOffset;
			RenderBuffer = renderBuffer ?? RenderBuffer;

			PixelWorker = pixelWorker ?? PixelWorker;
			SamplePattern = samplePattern ?? SamplePattern;

			Interlocked.Exchange(ref _initiatedSample, 0);
			Interlocked.Exchange(ref _completedSample, 0);
		}

		public void Dispatch()
		{
			if (Working) throw new Exception("Worker already dispatched!");

			if (PixelWorker == null) throw ExceptionHelper.Invalid(nameof(PixelWorker), InvalidType.isNull);
			if (RenderBuffer == null) throw ExceptionHelper.Invalid(nameof(RenderBuffer), InvalidType.isNull);
			if (SamplePattern == null) throw ExceptionHelper.Invalid(nameof(SamplePattern), InvalidType.isNull);

			if (!worker.IsAlive) worker.Start();
			resetEvent.Set();
		}

		void WorkThread()
		{
			while (!disposed)
			{
				resetEvent.Wait();

				try
				{
					Parallel.For(0, sampleCount, WorkSample);   //Render samples
					Parallel.For(0, pixels.Length, StorePixel); //Store pixels to buffer
				}
				finally { resetEvent.Reset(); }

				OnWorkCompleted?.Invoke(this);
			}
		}

		void WorkSample(long sample)
		{
			Interlocked.Increment(ref _initiatedSample);

			int pixelIndex = (int)(sample % pixels.Length);
			Int2 position = ToBufferPosition(pixelIndex);

			if (IsValid(position))
			{
				//Render pixel
				Float2 uv = ToAdjustedUV(position, sample);
				Float3 color = PixelWorker.Render(uv); //UV is adjusted to the correct scaling to match worker's requirement

				//Write to pixels
				writeSemaphore.Wait();
				try
				{
					ref Pixel pixel = ref pixels[pixelIndex];
					pixel.Accumulate(color);
				}
				finally { writeSemaphore.Release(); }
			}

			Interlocked.Increment(ref _completedSample);
		}

		void StorePixel(int pixelIndex)
		{
			Int2 position = ToBufferPosition(pixelIndex);
			if (!IsValid(position)) return;

			Shade color = pixels[pixelIndex].Color;
			RenderBuffer.SetPixel(position, color);
		}

		Int2 ToBufferPosition(int pixelIndex)
		{
			int x = pixelIndex / size + RenderOffset.x;
			int y = pixelIndex % size + RenderOffset.y;
			return new Int2(x, y);
		}

		Float2 ToAdjustedUV(Int2 bufferPosition, long sample)
		{
			Float2 offset = SamplePattern[(int)(sample / pixels.Length)];
			Float2 uv = RenderBuffer.ToUV(bufferPosition + offset);
			return new Float2(uv.x - 0.5f, (uv.y - 0.5f) / RenderBuffer.aspect);
		}

		/// <summary>
		/// Check to make sure the <paramref name="bufferPosition"/> is inside our canvas; parts of some edge tiles might be outside.
		/// </summary>
		bool IsValid(Int2 bufferPosition) => bufferPosition.x >= 0 && bufferPosition.y >= 0 && bufferPosition.x < RenderBuffer.size.x && bufferPosition.y < RenderBuffer.size.y;

		public override int GetHashCode() => id;
		public override string ToString() => $"Tile Worker {size}x{size} #{id}";

		public void Dispose()
		{
			if (disposed) return;
			disposed = true;

			resetEvent?.Dispose();
			writeSemaphore?.Dispose();
		}

		struct Pixel
		{
			double r;
			double g;
			double b;

			int accumulation;

			public Shade Color => new Shade((float)(r / accumulation), (float)(g / accumulation), (float)(b / accumulation));

			public void Accumulate(Float3 value)
			{
				r += value.x;
				g += value.y;
				b += value.z;

				accumulation++;
			}
		}
	}
}