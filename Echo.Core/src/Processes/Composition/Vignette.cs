using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Processes.Composition;

public record Vignette : ICompositeLayer
{
	/// <summary>
	/// The label of the layer to operate on.
	/// </summary>
	public string TargetLayer { get; init; } = "main";

	/// <summary>
	/// The strength of the darkening near the edge of the image.
	/// </summary>
	public float Intensity { get; init; } = 0.22f;

	public ComputeTask ExecuteAsync(CompositeContext context)
	{
		if (!context.TryGetBuffer(TargetLayer, out SettableGrid<RGB128> sourceBuffer)) return ComputeTask.CompletedTask;

		return context.RunAsync(MainPass, sourceBuffer.size);

		void MainPass(Int2 position)
		{
			float distance = Float2.Distance(sourceBuffer.ToUV(position), Float2.Half) * Scalars.Root2;
			sourceBuffer.Set(position, sourceBuffer[position] * (1f - Curves.Sigmoid(distance) * Intensity));
		}
	}
}