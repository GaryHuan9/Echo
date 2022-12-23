using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Processes.Composition;

public record Vignette : ICompositionLayer
{
	public string BufferLabel { get; init; } = "main";
	
	public float Intensity { get; init; } = 0.57f;
	public float FilmGrain { get; init; } = 0.01f; //A little bit of film grain helps with the color banding

	public ComputeTask ExecuteAsync(CompositeContext context)
	{
		if (!context.TryGetBuffer(BufferLabel, out SettableGrid<RGB128> buffer)) return ComputeTask.CompletedTask;
		
		return context.RunAsync(MainPass, buffer.size.Y);

		void MainPass(uint y)
		{
			var random = new SquirrelPrng(y);

			for (int x = 0; y < buffer.size.X; y++)
			{
				Int2 position = new Int2(x, (int)y);
				Float2 uv = position * buffer.sizeR;

				float distance = (uv - Float2.Half).SquaredMagnitude * Intensity;
				float multiplier = random.Next1(-FilmGrain, FilmGrain) - distance;

				buffer.Set(position, buffer[position] * (1f + multiplier));
			}
		}
	}
}