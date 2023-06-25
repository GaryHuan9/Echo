using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Processes.Composition;

[EchoSourceUsable]
public record Vignette : ICompositeLayer
{
	/// <summary>
	/// The label of the layer to operate on.
	/// </summary>
	[EchoSourceUsable]
	public string TargetLayer { get; init; } = "main";

	/// <summary>
	/// The strength of the darkening near the edge of the image.
	/// </summary>
	[EchoSourceUsable]
	public float Intensity { get; init; } = 0.25f;

	/// <inheritdoc/>
	[EchoSourceUsable]
	public bool Enabled { get; init; } = true;

	public ComputeTask ExecuteAsync(ICompositeContext context)
	{
		var sourceTexture = context.GetWriteTexture<RGB128>(TargetLayer);

		return context.RunAsync(MainPass, sourceTexture.size);

		void MainPass(Int2 position)
		{
			float distance = Float2.Distance(sourceTexture.ToUV(position), Float2.Half) * Scalars.Root2;
			sourceTexture.Set(position, sourceTexture.Get(position) * (1f - Curves.Sigmoid(distance) * Intensity));
		}
	}
}