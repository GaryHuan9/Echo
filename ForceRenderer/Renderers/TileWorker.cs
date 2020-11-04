using System.Threading;
using CodeHelpers;
using CodeHelpers.Vectors;

namespace ForceRenderer.Renderers
{
	/// <summary>
	/// A worker class that process/render on a specific tile.
	/// Required resources should be provided to the worker first, then the worker is dispatched.
	/// After the work is done, the worker will wait for the resources and completed work to be swapped out and restart processing.
	/// </summary>
	public class TileWorker
	{
		public TileWorker(int pixelSample, int size)
		{
			id = Interlocked.Increment(ref workerIdAccumulator);
			sampleCount = (long)size * size * pixelSample;

			pixels = new Pixel[size * size];
			worker = new Thread(WorkerThread)
					 {
						 IsBackground = true,
						 Priority = ThreadPriority.Highest,
						 Name = $"Tile Worker {size}x{size} #{id}"
					 };
		}

		public readonly int id;
		public readonly long sampleCount;

		public int RenderOffset { get; private set; }
		public long CompletedSample { get; private set; }

		public PixelWorker PixelWorker { get; private set; }

		readonly Pixel[] pixels;
		readonly Thread worker;

		readonly AutoResetEvent autoResetEvent = new AutoResetEvent(false);
		static volatile int workerIdAccumulator;

		public void ConfigureParameters()
		{

		}

		public void Dispatch()
		{

		}

		void WorkerThread()
		{
			while (true)
			{
				autoResetEvent.WaitOne();
			}
		}

		public override int GetHashCode() => id;

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