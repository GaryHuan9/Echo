using System;
using System.Threading;
using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Processes.Composition;

public static class CommonOperation
{
	public static async ComputeTask<float> GrabLuminance(ExecuteContext context, TextureGrid<RGB128> sourceBuffer)
	{
		var locker = new SpinLock();
		var total = Summation.Zero;

		await context.RunAsync(MainPass, sourceBuffer.size.Y);

		return ((RGB128)total.Result).Luminance / sourceBuffer.size.Product;

		void MainPass(uint y)
		{
			var row = Summation.Zero;

			for (int x = 0; x < sourceBuffer.size.X; x++) row += sourceBuffer[new Int2(x, (int)y)];

			bool lockTaken = false;

			try
			{
				locker.Enter(ref lockTaken);
				total += row;
			}
			finally
			{
				if (lockTaken) locker.Exit();
			}
		}
	}

	public static async ComputeTask GaussianBlur(ExecuteContext context, SettableGrid<RGB128> sourceBuffer, float deviation = 1f, int quality = 4)
	{
		Int2 size = sourceBuffer.size;
		int[] radii = new int[quality];
		FillGaussianRadii(deviation, radii, out float actual);

		using var _ = context.FetchTemporaryBuffer(out var workerBuffer, size);

		int radius;
		float diameterR;

		for (int i = 0; i < quality; i++)
		{
			radius = radii[i];
			diameterR = 1f / (radius * 2 + 1);

			await context.RunAsync(BlurPassX, size.X); //Write to workerBuffer
			await context.RunAsync(BlurPassY, size.Y); //Write to sourceBuffer
		}

		void BlurPassX(uint x)
		{
			var accumulator = Summation.Zero;

			for (int y = -radius; y < radius; y++) accumulator += Get(y);

			for (int y = 0; y < size.Y; y++)
			{
				accumulator += Get(y + radius);

				workerBuffer.Set(new Int2((int)x, y), (RGB128)(accumulator.Result * diameterR));

				accumulator -= Get(y - radius);
			}

			RGB128 Get(int y) => sourceBuffer[new Int2((int)x, y.Clamp(0, size.Y - 1))];
		}

		void BlurPassY(uint y)
		{
			var accumulator = Summation.Zero;

			for (int x = -radius; x < radius; x++) accumulator += Get(x);

			for (int x = 0; x < size.X; x++)
			{
				accumulator += Get(x + radius);

				sourceBuffer.Set(new Int2(x, (int)y), (RGB128)(accumulator.Result * diameterR));

				accumulator -= Get(x - radius);
			}

			RGB128 Get(int x) => workerBuffer[new Int2(x.Clamp(0, size.X - 1), (int)y)];
		}
	}

	static void FillGaussianRadii(float deviation, Span<int> radii, out float actual)
	{
		//Calculate Gaussian approximation convolution square size
		//Based on: http://blog.ivank.net/fastest-gaussian-blur.html

		float alpha = deviation * deviation;
		int quality = radii.Length;

		int beta = (int)MathF.Sqrt(12f * alpha / quality + 1f);
		if (beta % 2 == 0) beta--;

		float gamma = quality * beta * beta - 4f * quality * beta - 3f * quality;
		int threshold = ((12f * alpha - gamma) / (-4f * beta - 4f)).Round();

		//Record radii
		for (int i = 0; i < quality; i++)
		{
			int size = i < threshold ? beta : beta + 2;
			radii[i] = (size - 1) / 2;
		}

		//Calculate actual deviation
		actual = (quality - threshold) * (beta + 2) * (beta + 2) - quality;
		actual = MathF.Sqrt((threshold * beta * beta + actual) / 12f);
	}
}