using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Processes.Composition;

public record Bloom : ICompositionLayer
{
	public string TargetLayer { get; init; } = "main";

	public float Intensity { get; set; } = 0.88f;
	public float Threshold { get; set; } = 0.95f;

	public async ComputeTask ExecuteAsync(CompositionContext context)
	{
		if (!context.TryGetBuffer(TargetLayer, out SettableGrid<RGB128> sourceBuffer)) return;
		using var _ = context.FetchTemporaryBuffer(out ArrayGrid<RGB128> workerBuffer);

		//Fill filtered color values to workerBuffer
		await context.RunAsync(FilterPass);

		//Run Gaussian blur on workerBuffer
		float deviation = sourceBuffer.LogSize / 64f;
		await context.GaussianBlur(workerBuffer, deviation);

		//Combine blurred workerBuffer with renderBuffer
		await context.RunAsync(CombinePass);

		void FilterPass(Int2 position)
		{
			RGB128 source = sourceBuffer[position];
			float luminance = source.Luminance;

			if (luminance <= Threshold)
			{
				workerBuffer.Set(position, RGB128.Black);
				return;
			}

			float excess = luminance - Threshold;
			RGB128 normal = source / luminance;

			workerBuffer.Set(position, normal * excess * Intensity);
			sourceBuffer.Set(position, normal * Threshold);
		}

		void CombinePass(Int2 position)
		{
			RGB128 source = workerBuffer[position];
			RGB128 target = sourceBuffer[position];
			sourceBuffer.Set(position, target + source);
		}
	}
}