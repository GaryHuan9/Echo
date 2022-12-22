using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Textures.Evaluation;

/// <summary>
/// A tile-based evaluation destination. Used by <see cref="IEvaluationLayer"/>.
/// </summary>
public interface IEvaluationTile
{
	/// <summary>
	/// The minimum pixel position of this <see cref="IEvaluationTile"/> (inclusive).
	/// </summary>
	public Int2 Min { get; }

	/// <summary>
	/// The maximum pixel position of this <see cref="IEvaluationTile"/> (exclusive).
	/// </summary>
	public Int2 Max { get; }

	/// <summary>
	/// The size of this <see cref="IEvaluationTile"/>.
	/// </summary>
	/// <remarks>This could be smaller than <see cref="EvaluationLayer{T}.tileSize"/>.</remarks>
	public sealed Int2 Size => Max - Min;
}

/// <summary>
/// A variant of <see cref="IEvaluationTile"/> that allows writing to the tile.
/// </summary>
public interface IEvaluationWriteTile : IEvaluationTile
{
	/// <summary>
	/// Writes the value of a pixel to this <see cref="IEvaluationWriteTile"/>.
	/// </summary>
	/// <param name="position">The pixel position to write at.</param>
	public Float4 this[Int2 position] { set; }
}

/// <summary>
/// A variant of <see cref="IEvaluationTile"/> that allows reading from the tile.
/// </summary>
public interface IEvaluationReadTile : IEvaluationTile
{
	/// <summary>
	/// Reads the value of a pixel from this <see cref="IEvaluationReadTile"/>.
	/// </summary>
	/// <param name="position">The pixel position to read from.</param>
	public RGBA128 this[Int2 position] { get; }
}