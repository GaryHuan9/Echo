using System;
using System.Runtime.CompilerServices;
using System.Threading;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Pixels;
using EchoRenderer.Textures.DimensionTwo;

namespace EchoRenderer.Rendering.Engines
{
	/// <summary>
	/// A worker class that process/render on a specific tile.
	/// Required parameters should be provided to the worker first, then the worker is dispatched.
	/// After the work is done, the worker will wait for parameters and completed work to be swapped out and restart processing.
	/// </summary>
	public class TileWorker : IDisposable
	{
		public TileWorker(TiledRenderProfile profile)
		{
			id = Interlocked.Increment(ref workerIdAccumulator);
			size = profile.TileSize;

			renderBuffer = profile.RenderBuffer;
			pixelWorker = profile.Method;

			pixelSample = profile.PixelSample;
			adaptiveSample = profile.AdaptiveSample;

			//Create golden ratio square spiral offsets
			spiralOffsets = new Float2[pixelSample];

			for (int i = 0; i < spiralOffsets.Length; i++)
			{
				float theta = Scalars.TAU * Scalars.GoldenRatio * i;
				Float2 offset = new Float2(MathF.Cos(theta), MathF.Sin(theta));

				float square = Math.Abs(MathF.Cos(theta + Scalars.PI / 4)) + Math.Abs(MathF.Sin(theta + Scalars.PI / 4));
				float radius = MathF.Sqrt((i + 0.5f) / spiralOffsets.Length) * Scalars.Sqrt2 / square / 2f;

				spiralOffsets[i] = offset * radius + Float2.half;
			}

			//Allocate thread
			worker = new Thread(WorkThread)
					 {
						 IsBackground = true,
						 Name = $"Tile Worker #{id} {size}x{size}"
					 };

			//Create arena for thread. NOTE that HashCode returns a different value every runtime!
			int seed = HashCode.Combine(Environment.TickCount64, id, size);
			arena = pixelWorker.CreateArena(profile, (uint)seed);
		}

		readonly int id;
		readonly int size;

		readonly RenderBuffer renderBuffer;
		readonly PixelWorker pixelWorker;

		readonly int pixelSample;
		readonly int adaptiveSample;

		int _renderOffsetX;
		int _renderOffsetY;
		long _totalPixel;

		public int RenderOffsetX => InterlockedHelper.Read(ref _renderOffsetX);
		public int RenderOffsetY => InterlockedHelper.Read(ref _renderOffsetY);

		public Int2 RenderOffset => new(RenderOffsetX, RenderOffsetY);
		public long TotalPixel => Interlocked.Read(ref _totalPixel);

		long _completedSample;
		long _completedPixel;
		long _rejectedSample;

		public long CompletedSample => Interlocked.Read(ref _completedSample);
		public long CompletedPixel => Interlocked.Read(ref _completedPixel);
		public long RejectedSample => Interlocked.Read(ref _rejectedSample);

		readonly Thread worker;
		readonly Arena arena;

		/// <summary>
		/// Offset applied to each pixel during regular pixel sampling.
		/// </summary>
		readonly Float2[] spiralOffsets;

		readonly ManualResetEventSlim dispatchEvent = new(); //Event sets when the worker is dispatched
		public bool Working => dispatchEvent.IsSet;

		static volatile int workerIdAccumulator;
		bool aborted;

		/// <summary>
		/// Invoked on worker thread when all rendered pixels are stored in the buffer.
		/// Passes in the <see cref="TileWorker"/> for easy identification.
		/// </summary>
		public event Action<TileWorker> OnWorkCompletedMethods;

		public void Reset(Int2 renderOffset)
		{
			if (aborted) throw new Exception("Worker already aborted! It should not be used anymore!");
			if (Working) throw new Exception("Cannot reset when the worker is dispatched and already working!");

			Interlocked.Exchange(ref _renderOffsetX, renderOffset.x);
			Interlocked.Exchange(ref _renderOffsetY, renderOffset.y);

			Int2 boarder = renderBuffer.size.Min(RenderOffset + (Int2)size); //Get the furthest buffer position
			Interlocked.Exchange(ref _totalPixel, (boarder - RenderOffset).Product);

			Interlocked.Exchange(ref _completedSample, 0);
			Interlocked.Exchange(ref _completedPixel, 0);
			Interlocked.Exchange(ref _rejectedSample, 0);
		}

		public void Dispatch()
		{
			if (aborted) throw new Exception("Worker already aborted!");
			if (Working) throw new Exception("Worker already dispatched!");

			if (!worker.IsAlive) worker.Start();
			dispatchEvent.Set();
		}

		void WorkThread()
		{
			while (!aborted)
			{
				dispatchEvent.Wait();
				if (aborted) goto end;

				try
				{
					for (int x = 0; x < size; x++)
					{
						for (int y = 0; y < size; y++)
						{
							if (aborted) goto end;
							WorkPixel(new Int2(x, y));
						}
					}
				}
				finally { dispatchEvent.Reset(); }

				if (!aborted) OnWorkCompletedMethods?.Invoke(this);
			}

			end:
			{ }
		}

		void WorkPixel(Int2 localPosition)
		{
			Int2 position = localPosition + RenderOffset;
			if (position.Clamp(Int2.zero, renderBuffer.oneLess) != position) return; //Ignore pixels outside of buffer

			float aspect = 1f / renderBuffer.aspect;
			RenderPixel pixel = new RenderPixel();

			//Regular pixel sampling
			for (int i = 0; i < pixelSample; i++) Sample(spiralOffsets[i]);

			//Change to adaptive sampling
			int sampleCount = (int)(pixel.Deviation * adaptiveSample);
			for (int i = 0; i < sampleCount; i++) Sample(arena.random.NextSample());

			//Store pixel
			pixel.Store(renderBuffer, position);
			Interlocked.Increment(ref _completedPixel);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void Sample(Float2 offset)
			{
				//Sample color
				Float2 uv = (position + offset) / renderBuffer.size - Float2.half;
				var sample = pixelWorker.Render(uv.ReplaceY(uv.y * aspect), arena);

				//Write to pixel
				bool successful = pixel.Accumulate(sample);
				Interlocked.Increment(ref _completedSample);

				if (!successful) Interlocked.Increment(ref _rejectedSample);
			}
		}

		public void Abort()
		{
			if (aborted) throw new Exception("Worker already aborted!");
			aborted = true;

			if (worker.IsAlive)
			{
				dispatchEvent.Set(); //Release the wait block
				worker.Join();
			}
		}

		public void Dispose()
		{
			if (!aborted) Abort();
			dispatchEvent.Dispose();
		}

		public override int    GetHashCode() => id;
		public override string ToString()    => $"Tile Worker #{id} {size}x{size}";
	}
}