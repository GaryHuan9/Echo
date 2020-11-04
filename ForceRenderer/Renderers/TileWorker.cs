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
		public TileWorker(int pixelSample, int size)
		{
			id = Interlocked.Increment(ref workerIdAccumulator);
			sampleCount = (long)size * size * pixelSample;
			this.size = size;

			pixels = new Pixel[size * size];
			worker = new Thread(WorkThread)
					 {
						 IsBackground = true,
						 Priority = ThreadPriority.Highest,
						 Name = $"Tile Worker {size}x{size} #{id}"
					 };
		}

		public readonly int id;
		public readonly long sampleCount;
		public readonly int size;

		public Int2 RenderOffset { get; private set; }
		public PixelWorker PixelWorker { get; private set; }
		public Texture RenderBuffer { get; private set; }

		long _completedSample;
		long _initiatedSample;

		public long CompletedSample => Interlocked.Read(ref _completedSample);
		public long InitiatedSample => Interlocked.Read(ref _initiatedSample);

		readonly Pixel[] pixels;
		readonly Thread worker;

		readonly ManualResetEventSlim resetEvent = new ManualResetEventSlim(); //Event sets when the worker is dispatched
		readonly SemaphoreSlim writeSemaphore = new SemaphoreSlim(0, 1);

		public bool Working => resetEvent.IsSet;

		bool disposed;
		static volatile int workerIdAccumulator;

		/// <summary>
		/// Invoked on render thread when half of the samples are rendered.
		/// </summary>
		public event Action OnHalfCompleted;

		/// <summary>
		/// Invoked on render thread when all of the samples are rendered.
		/// </summary>
		public event Action OnFullCompleted;

		/// <summary>
		/// Invoked on worker thread when all rendered pixels are stored in the buffer.
		/// </summary>
		public event Action OnPixelsBuffered;

		public void ResetParameters(Int2 renderOffset, PixelWorker pixelWorker = null, Texture renderBuffer = null)
		{
			if (pixelWorker == null && PixelWorker == null) throw ExceptionHelper.Invalid(nameof(pixelWorker), InvalidType.isNull);
			if (renderBuffer == null && RenderBuffer == null) throw ExceptionHelper.Invalid(nameof(renderBuffer), InvalidType.isNull);
			if (Working) throw new Exception("Cannot reset when the worker is dispatched and already working!");

			RenderOffset = renderOffset;
			PixelWorker = pixelWorker;
			RenderBuffer = renderBuffer;

			Interlocked.Exchange(ref _initiatedSample, 0);
			Interlocked.Exchange(ref _completedSample, 0);
		}

		public void Dispatch()
		{
			if (Working) throw new Exception("Worker already dispatched!");

			if (PixelWorker == null) throw ExceptionHelper.Invalid(nameof(PixelWorker), InvalidType.isNull);
			if (RenderBuffer == null) throw ExceptionHelper.Invalid(nameof(RenderBuffer), InvalidType.isNull);

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

				OnPixelsBuffered?.Invoke();
			}
		}

		void WorkSample(long sample)
		{
			Interlocked.Increment(ref _initiatedSample);
			int pixelIndex = (int)(sample % pixels.Length);

			//Render pixel
			Int2 position = ToBufferPosition(pixelIndex); //TODO: Add spiral offset
			Float3 color = PixelWorker.Render(RenderBuffer.ToUV(position));

			//Write to pixels
			writeSemaphore.Wait();
			try
			{
				ref Pixel pixel = ref pixels[pixelIndex];
				pixel.Accumulate(color);
			}
			finally { writeSemaphore.Wait(); }

			//Finalize
			long order = Interlocked.Increment(ref _completedSample);

			if (order == sampleCount) OnFullCompleted?.Invoke();
			if (order == sampleCount / 2L) OnHalfCompleted?.Invoke();
		}

		void StorePixel(int pixelIndex)
		{
			Shade color = pixels[pixelIndex].Color;
			RenderBuffer.SetPixel(ToBufferPosition(pixelIndex), color);
		}

		Int2 ToBufferPosition(int pixelIndex)
		{
			int x = pixelIndex / size + RenderOffset.x;
			int y = pixelIndex % size + RenderOffset.y;
			return new Int2(x, y);
		}

		public override int GetHashCode() => id;

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