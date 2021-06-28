using System;
using System.Threading;
using System.Threading.Tasks;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using EchoRenderer.Mathematics;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.PostProcessing.Operators
{
	public class LuminanceGrab
	{
		public LuminanceGrab(PostProcessingWorker worker, Texture2D sourceBuffer)
		{
			this.worker = worker;
			this.sourceBuffer = sourceBuffer;
		}

		public float Luminance { get; private set; }

		readonly PostProcessingWorker worker;
		readonly Texture2D sourceBuffer;

		double luminanceTotal;

		static int HeightThreshold => Environment.ProcessorCount * 2;

		public void Run()
		{
			Int2 size = sourceBuffer.size;
			double length = size.Product;

			Interlocked.Exchange(ref luminanceTotal, 0d);

			//Because the work of grabbing luminance on each position is relatively small,
			//we selectively run either a full or vertical pass to maximize performance.

			if (size.y < HeightThreshold) worker.RunPass(LuminancePass, sourceBuffer);
			else worker.RunPassVertical(VerticalLuminancePass, sourceBuffer);

			Luminance = (float)(InterlockedHelper.Read(ref luminanceTotal) / length);
		}

		void LuminancePass(Int2 position)
		{
			float luminance = Utilities.GetLuminance(sourceBuffer[position]);
			InterlockedHelper.Add(ref luminanceTotal, luminance);
		}

		void VerticalLuminancePass(int vertical)
		{
			double luminance = 0d;

			for (int x = 0; x < sourceBuffer.size.x; x++)
			{
				var color = sourceBuffer[new Int2(x, vertical)];
				luminance += Utilities.GetLuminance(color);
			}

			InterlockedHelper.Add(ref luminanceTotal, luminance);
		}
	}
}