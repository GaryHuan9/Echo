using Echo.Core.Evaluation.Engines;

namespace Echo.Core.Evaluation.Operations;

public record TiledEvaluationProfile : EvaluationProfile
{
	/// <summary>
	/// The size of one square tile.
	/// </summary>
	public int TileSize { get; init; } = 32;

	/// <summary>
	/// The <see cref="ITilePattern"/> used to determine the sequence of the tiles.
	/// </summary>
	public ITilePattern Pattern { get; init; }
}