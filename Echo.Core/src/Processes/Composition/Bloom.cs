using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Processes.Composition;

public record Bloom : ICompositeLayer
{
	/// <summary>
	/// The label of the layer to operate on.
	/// </summary>
	public string TargetLayer { get; init; } = "main";

	/// <summary>
	/// The amount of excess luminance distributed to neighboring pixels. 
	/// </summary>
	public float Intensity { get; set; } = 0.25f;

	/// <summary>
	/// Pixels with a luminance higher than this value will cause bloom.
	/// </summary>
	public float Threshold { get; set; } = 0.98f;

	public async ComputeTask ExecuteAsync(ICompositeContext context)
	{
		SettableGrid<RGB128> sourceTexture = context.GetWriteTexture<RGB128>(TargetLayer);
		using var _ = context.FetchTemporaryTexture(out ArrayGrid<RGB128> workerTexture);

		//Fill filtered color values to workerTexture
		await context.RunAsync(FilterPass);

		//Run Gaussian blur on workerTexture
		float deviation = sourceTexture.LogSize / 64f;
		await context.GaussianBlurAsync(workerTexture, deviation);

		//Combine blurred workerTexture with sourceTexture
		await context.RunAsync(CombinePass);

		void FilterPass(Int2 position)
		{
			RGB128 source = sourceTexture[position];
			float luminance = source.Luminance;

			if (luminance <= Threshold)
			{
				workerTexture.Set(position, RGB128.Black);
				return;
			}

			float excess = luminance - Threshold;
			RGB128 normal = source / luminance;

			workerTexture.Set(position, normal * excess * Intensity);
			sourceTexture.Set(position, normal * Threshold);
		}

		void CombinePass(Int2 position)
		{
			RGB128 source = workerTexture[position];
			RGB128 target = sourceTexture[position];
			sourceTexture.Set(position, target + source);
		}
	}
}