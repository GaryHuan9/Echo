using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Processes.Composition;

/// <summary>
/// Post processing depth of field effect. Faster than path traced depth of field.
/// </summary>
public record DepthOfField : ICompositeLayer
{
	/// <summary>
	/// The label of the layer to operate on.
	/// </summary>
	public string TargetLayer { get; init; } = "main";

	/// <summary>
	/// The label of the <see cref="NormalDepth128"/> layer to read from.
	/// </summary>
	public string DepthLayer { get; init; } = "normal-depth";

	/// <summary>
	/// The strength of the confusion.
	/// </summary>
	public float Intensity { get; set; } = 1f;

	/// <summary>
	/// The nearer distance from which the blur begins to fade.
	/// </summary>
	public float NearStart { get; set; } = 0f;
	
	/// <summary>
	/// The nearer distance border when depth of field stops.
	/// </summary>
	public float NearEnd { get; set; } = 2f;

	/// <summary>
	/// The farther distance border until where there is no depth of field.
	/// </summary>
	public float FarStart { get; set; } = 15f;
	
	/// <summary>
	/// The farther distance at which the blur is at its fullest.
	/// </summary>
	public float FarEnd { get; set; } = 20f;

	public async ComputeTask ExecuteAsync(ICompositeContext context)
	{
		var sourceTexture = context.GetWriteTexture<RGB128>(TargetLayer);
		var depthTexture = context.GetReadTexture<NormalDepth128>(DepthLayer);
		using var _ = context.FetchTemporaryTexture(out ArrayGrid<RGB128> workerTexture);

		await context.CopyAsync(sourceTexture, workerTexture);
		float deviation = sourceTexture.LogSize / 64f * Intensity;
		await context.GaussianBlurAsync(workerTexture, deviation);
		await context.RunAsync(MainPass);

		void MainPass(Int2 position)
		{
			float depth = depthTexture[position].depth;
			float near = FastMath.Clamp01(Scalars.InverseLerp(NearEnd, NearStart, depth));
			float far = FastMath.Clamp01(Scalars.InverseLerp(FarEnd, FarStart, depth));

			RGB128 source = sourceTexture[position];
			RGB128 blurred = workerTexture[position];
			float clearness = Curves.Sigmoid(near - far);
			
			sourceTexture.Set(position, (RGB128)Float4.Lerp(blurred, source, clearness));
		}
	}
}