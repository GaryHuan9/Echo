using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Packed;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Processes.Composition;

[EchoSourceUsable]
public record Bloom : ICompositeLayer
{
	/// <summary>
	/// The label of the layer to operate on.
	/// </summary>
	[EchoSourceUsable]
	public string TargetLayer { get; init; } = "main";

	/// <summary>
	/// The amount of excess luminance distributed to neighboring pixels. 
	/// </summary>
	[EchoSourceUsable]
	public float Intensity { get; set; } = 0.25f;

	/// <summary>
	/// Pixels with a luminance higher than this value will cause bloom.
	/// </summary>
	[EchoSourceUsable]
	public float Threshold { get; set; } = 0.98f;

	/// <inheritdoc/>
	[EchoSourceUsable]
	public bool Enabled { get; init; } = true;

	public async ComputeTask ExecuteAsync(ICompositeContext context)
	{
		SettableGrid<RGB128> sourceTexture = context.GetWriteTexture<RGB128>(TargetLayer);
		using var _ = context.FetchTemporaryTexture(out ArrayGrid<RGB128> workerTexture);

		await context.RunAsync(FilterPass, sourceTexture.size);  //Fill filtered color values to workerTexture
		await context.GaussianBlurAsync(workerTexture, 4f);      //Run Gaussian blur on workerTexture
		await context.RunAsync(CombinePass, sourceTexture.size); //Combine blurred workerTexture with sourceTexture

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