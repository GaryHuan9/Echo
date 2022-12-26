using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Mathematics.Randomization;
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
	public float Intensity { get; init; } = 0.16f;
	
	/// <summary>
	/// Small random changes to the brightness of the pixels. 
	/// </summary>
	/// <remarks>A little bit of grain helps with the color banding</remarks>
	public float FilmGrain { get; init; } = 0.01f;

	public ComputeTask ExecuteAsync(CompositeContext context)
	{
		if (!context.TryGetBuffer(TargetLayer, out SettableGrid<RGB128> buffer)) return ComputeTask.CompletedTask;

		return context.RunAsync(MainPass, buffer.size.Y);

		void MainPass(uint y)
		{
			var random = new SquirrelPrng(y);

			for (int x = 0; x < buffer.size.X; x++)
			{
				Int2 position = new Int2(x, (int)y);

				float scale = buffer.SquaredCenterDistance(position) * Intensity;
				float multiplier = random.Next1(-FilmGrain, FilmGrain) - scale;

				buffer.Set(position, buffer[position] * (1f + multiplier));
			}
		}
	}
}