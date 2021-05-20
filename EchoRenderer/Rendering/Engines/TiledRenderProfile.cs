using CodeHelpers;
using EchoRenderer.Rendering.Engines.Tiles;

namespace EchoRenderer.Rendering.Engines
{
	public record TiledRenderProfile : RenderProfile
	{
		/// <summary>
		/// The tile pattern used to determine the order of tiles rendered.
		/// </summary>
		public ITilePattern TilePattern { get; init; }

		/// <summary>
		/// The size of one square tile.
		/// </summary>
		public int TileSize { get; init; } = 32;

		/// <summary>
		/// The base/minimum number of samples calculated for each pixel.
		/// This first sample pass also determines the sample size for the second pass.
		/// </summary>
		public int PixelSample { get; init; }

		/// <summary>
		/// A multiplier that affects the sample size for the second sample pass.
		/// NOTE: The actual count is determined by the first pass and it can be larger than this value!
		/// </summary>
		public int AdaptiveSample { get; init; }

		public override void Validate()
		{
			base.Validate();

			if (TilePattern == null) throw ExceptionHelper.Invalid(nameof(TilePattern), InvalidType.isNull);
			if (TileSize <= 0) throw ExceptionHelper.Invalid(nameof(TileSize), TileSize, InvalidType.outOfBounds);

			if (PixelSample <= 0) throw ExceptionHelper.Invalid(nameof(PixelSample), PixelSample, InvalidType.outOfBounds);
			if (AdaptiveSample < 0) throw ExceptionHelper.Invalid(nameof(AdaptiveSample), AdaptiveSample, InvalidType.outOfBounds);
		}
	}
}