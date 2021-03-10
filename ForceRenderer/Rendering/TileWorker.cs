using System;
using System.Threading;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using ForceRenderer.Textures;

namespace ForceRenderer.Rendering
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
			id = Interlocked.Increment(ref workerIdAccumulator);
			size = profile.tileSize;

			pixelSample = profile.pixelSample;
			adaptiveSample = profile.adaptiveSample;

			worker = new Thread(WorkThread)
					 {
						 IsBackground = true,
						 Name = $"Tile Worker #{id} {size}x{size}"
					 };

			spiralOffsets = new Float2[pixelSample];
			randomOffsets = new Float2[adaptiveSample * 3]; //Prepare more offsets than sample because adaptive sample might go beyond the setting

			//Create golden ratio square spiral offsets
			for (int i = 0; i < spiralOffsets.Length; i++)
			{
				float theta = Scalars.TAU * Scalars.GoldenRatio * i;
				Float2 offset = new Float2(MathF.Cos(theta), MathF.Sin(theta));

				float square = 1f / (Math.Abs(MathF.Cos(theta + Scalars.PI / 4)) + Math.Abs(MathF.Sin(theta + Scalars.PI / 4)));
				float radius = MathF.Sqrt((i + 0.5f) / spiralOffsets.Length) * Scalars.Sqrt2 * square / 2f;

				spiralOffsets[i] = offset * radius + Float2.half;
			}

			//Create random offsets
			Random random = new Random(unchecked((int)(Environment.TickCount64 * id * size)));
			for (int i = 0; i < randomOffsets.Length; i++) randomOffsets[i] = new Float2((float)random.NextDouble(), (float)random.NextDouble());
		}

		readonly int id;
		readonly int size;

		readonly int pixelSample;
		readonly int adaptiveSample;

		int _renderOffsetX;
		int _renderOffsetY;
		long _totalPixel;

		public int RenderOffsetX => InterlockedHelper.Read(ref _renderOffsetX);
		public int RenderOffsetY => InterlockedHelper.Read(ref _renderOffsetY);

		public Int2 RenderOffset => new Int2(RenderOffsetX, RenderOffsetY);
		public long TotalPixel => Interlocked.Read(ref _totalPixel);

		Texture _renderBuffer;
		PixelWorker _pixelWorker;

		public Texture RenderBuffer => InterlockedHelper.Read(ref _renderBuffer);
		public PixelWorker PixelWorker => InterlockedHelper.Read(ref _pixelWorker);

		long _completedSample;
		long _completedPixel;
		long _rejectedSample;

		public long CompletedSample => Interlocked.Read(ref _completedSample);
		public long CompletedPixel => Interlocked.Read(ref _completedPixel);
		public long RejectedSample => Interlocked.Read(ref _rejectedSample);

		readonly Thread worker;

		//Offsets applied within each pixel
		readonly Float2[] spiralOffsets;
		readonly Float2[] randomOffsets;

		readonly ManualResetEventSlim dispatchEvent = new ManualResetEventSlim(); //Event sets when the worker is dispatched
		public bool Working => dispatchEvent.IsSet;

		static volatile int workerIdAccumulator;
		bool aborted;

		/// <summary>
		/// Invoked on worker thread when all rendered pixels are stored in the buffer.
		/// Passes in the <see cref="TileWorker"/> for easy identification.
		/// </summary>
		public event Action<TileWorker> OnWorkCompleted;

		public void ResetParameters(Int2 renderOffset, Texture renderBuffer = null, PixelWorker pixelWorker = null)
		{
			if (aborted) throw new Exception("Worker already aborted! It should not be used anymore!");
			if (Working) throw new Exception("Cannot reset when the worker is dispatched and already working!");

			Interlocked.Exchange(ref _renderOffsetX, renderOffset.x);
			Interlocked.Exchange(ref _renderOffsetY, renderOffset.y);

			if (renderBuffer != null) Interlocked.Exchange(ref _renderBuffer, renderBuffer);
			if (pixelWorker != null) Interlocked.Exchange(ref _pixelWorker, pixelWorker);

			Int2 boarder = RenderBuffer.size.Min(RenderOffset + (Int2)size); //Get the furthest buffer position
			Interlocked.Exchange(ref _totalPixel, (boarder - RenderOffset).Product);

			Interlocked.Exchange(ref _completedSample, 0);
			Interlocked.Exchange(ref _completedPixel, 0);
			Interlocked.Exchange(ref _rejectedSample, 0);
		}

		public void Dispatch()
		{
			if (aborted) throw new Exception("Worker already aborted!");
			if (Working) throw new Exception("Worker already dispatched!");

			if (PixelWorker == null) throw ExceptionHelper.Invalid(nameof(PixelWorker), InvalidType.isNull);
			if (RenderBuffer == null) throw ExceptionHelper.Invalid(nameof(RenderBuffer), InvalidType.isNull);

			if (!worker.IsAlive) worker.Start();
			dispatchEvent.Set();
		}

		void WorkThread()
		{
			while (!aborted)
			{
				dispatchEvent.Wait();
				if (aborted) break;

				try { Parallel.For(0, size * size, WorkPixel); }
				finally { dispatchEvent.Reset(); }

				if (!aborted) OnWorkCompleted?.Invoke(this);
			}
		}

		void WorkPixel(int index, ParallelLoopState state)
		{
			Int2 position = new Int2(index % size + RenderOffsetX, index / size + RenderOffsetY);
			if (!(position >= Int2.zero) || !(position < RenderBuffer.size)) return; //Reject pixels outside of buffer

			if (aborted) state.Break();
			Pixel pixel = new Pixel();

			int sampleCount = pixelSample;
			Float2[] uvOffsets = spiralOffsets;

			for (int m = 0; m < 2; m++)
			{
				for (int i = 0; i < sampleCount; i++)
				{
					if (aborted) state.Break();

					//Sample color
					Float2 uv = (position + uvOffsets[i % uvOffsets.Length]) / RenderBuffer.size - Float2.half;
					Float3 color = PixelWorker.Render(new Float2(uv.x, uv.y / RenderBuffer.aspect));

					//Write to pixel
					bool successful = pixel.Accumulate(color);
					Interlocked.Increment(ref _completedSample);

					if (!successful) Interlocked.Increment(ref _rejectedSample);
				}

				//Change to adaptive sampling
				sampleCount = (int)(pixel.Deviation * adaptiveSample);
				uvOffsets = randomOffsets;
			}

			if (aborted) state.Break();

			//Store pixel
			RenderBuffer.SetPixel(position, pixel.Color);
			Interlocked.Increment(ref _completedPixel);
		}

		public void Abort()
		{
			if (aborted) throw new Exception("Worker already aborted!");
			aborted = true;

			dispatchEvent.Set(); //Release the wait block
			worker.Join();
		}

		public void Dispose()
		{
			if (!aborted) Abort();
			dispatchEvent.Dispose();
		}

		public override int GetHashCode() => id;
		public override string ToString() => $"Tile Worker #{id} {size}x{size}";

		struct Pixel
		{
			Double3 average;
			Double3 squared;

			int accumulation;

			public const double MinDeviationThreshold = 0.3d;

			/// <summary>
			/// Returns the color average.
			/// </summary>
			public Float3 Color => (Float3)average;

			/// <summary>
			/// Returns the standard deviation of the pixel.
			/// Based on algorithm described here: https://nestedsoftware.com/2018/03/27/calculating-standard-deviation-on-streaming-data-253l.23919.html
			/// </summary>
			public double Deviation
			{
				get
				{
					double deviation = Math.Sqrt(squared.Average / accumulation);
					double max = Math.Max(average.Average, MinDeviationThreshold);

					return deviation / max;
				}
			}

			/// <summary>
			/// Accumulates the color <paramref name="value"/> to pixel.
			/// Returns false if the input was rejected because it was invalid.
			/// </summary>
			public bool Accumulate(Float3 value)
			{
				if (float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z)) return false; //NaN gate

				accumulation++;

				Double3 oldMean = average;
				Double3 newValue = value;

				average += (newValue - oldMean) / accumulation;
				squared += (newValue - average) * (newValue - oldMean);

				return true;
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
			public double Average => (x + y + z) / 3f;

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