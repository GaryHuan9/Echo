using CodeHelpers;

namespace EchoRenderer.Core.Rendering.Engines;

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

	public override void Validate()
	{
		base.Validate();

		if (TilePattern == null) throw ExceptionHelper.Invalid(nameof(TilePattern), InvalidType.isNull);
		if (TileSize <= 0) throw ExceptionHelper.Invalid(nameof(TileSize), TileSize, InvalidType.outOfBounds);
	}
}