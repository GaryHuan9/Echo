using CodeHelpers;

namespace Echo.Core.Evaluation.Operations;

public record TiledEvaluationProfile : EvaluationProfile
{
	/// <summary>
	/// The minimum number of epochs that must be performed before adaptive sampling begins.
	/// </summary>
	public int MinEpoch { get; init; } = 1;

	/// <summary>
	/// The maximum possible number of epochs that can be performed.
	/// </summary>
	public int MaxEpoch { get; init; } = 16;

	/// <summary>
	/// Evaluation is completed after noise is under this threshold.
	/// </summary>
	public float NoiseThreshold = 0.0005f;

	/// <summary>
	/// The size of one square tile.
	/// </summary>
	public int TileSize { get; init; } = 32;

	/// <summary>
	/// The <see cref="ITilePattern"/> used to determine the sequence of the tiles.
	/// </summary>
	public ITilePattern Pattern { get; init; } = new ScrambledPattern();

	public override void Validate()
	{
		base.Validate();

		if (MinEpoch <= 0) throw ExceptionHelper.Invalid(nameof(MinEpoch), MinEpoch, InvalidType.outOfBounds);
		if (MaxEpoch < MinEpoch) throw ExceptionHelper.Invalid(nameof(MaxEpoch), MaxEpoch, InvalidType.outOfBounds);
		if (NoiseThreshold < 0f) throw ExceptionHelper.Invalid(nameof(NoiseThreshold), NoiseThreshold, InvalidType.outOfBounds);

		if (TileSize <= 0) throw ExceptionHelper.Invalid(nameof(TileSize), TileSize, InvalidType.outOfBounds);
		if (Pattern == null) throw ExceptionHelper.Invalid(nameof(Pattern), InvalidType.isNull);
	}
}