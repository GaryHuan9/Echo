using CodeHelpers;
using Echo.Core.Evaluation.Evaluators;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Evaluation.Operation;

/// <summary>
/// An immutable profile to configure and define an <see cref="EvaluationOperation"/>.
/// </summary>
public record EvaluationProfile
{
	/// <summary>
	/// The fundamental evaluation method used.
	/// </summary>
	public Evaluator Evaluator { get; init; }

	/// <summary>
	/// The <see cref="ContinuousDistribution"/> used for this evaluation.
	/// </summary>
	/// <remarks>The <see cref="ContinuousDistribution.Extend"/> value specifies the precise number of samples used per epoch.</remarks>
	public ContinuousDistribution Distribution { get; init; }

	/// <summary>
	/// The destination <see cref="RenderBuffer"/> to store the evaluated data.
	/// </summary>
	/// <remarks>Use <see cref="RenderBuffer.tileSize"/> to configure the evaluation tile size.</remarks>
	public RenderBuffer Buffer { get; init; }

	/// <summary>
	/// The minimum number of epochs that must be performed before adaptive sampling begins.
	/// </summary>
	public int MinEpoch { get; init; } = 1;

	/// <summary>
	/// The maximum possible number of epochs that can be performed.
	/// </summary>
	public int MaxEpoch { get; init; } = 20;

	/// <summary>
	/// Evaluation is completed after noise is under this threshold.
	/// </summary>
	public float NoiseThreshold { get; init; } = 0.03f;

	/// <summary>
	/// The <see cref="ITilePattern"/> used to determine the sequence of the tiles.
	/// </summary>
	public ITilePattern Pattern { get; init; } = new HilbertCurvePattern();

	/// <summary>
	/// To be invoked to authenticate the validity of this <see cref="EvaluationProfile"/>.
	/// </summary>
	public void Validate()
	{
		if (Evaluator == null) throw ExceptionHelper.Invalid(nameof(Evaluator), InvalidType.isNull);
		if (Distribution == null) throw ExceptionHelper.Invalid(nameof(Distribution), InvalidType.isNull);
		if (Buffer == null) throw ExceptionHelper.Invalid(nameof(Buffer), InvalidType.isNull);

		if (MinEpoch <= 0) throw ExceptionHelper.Invalid(nameof(MinEpoch), MinEpoch, InvalidType.outOfBounds);
		if (MaxEpoch < MinEpoch) throw ExceptionHelper.Invalid(nameof(MaxEpoch), MaxEpoch, InvalidType.outOfBounds);
		if (NoiseThreshold < 0f) throw ExceptionHelper.Invalid(nameof(NoiseThreshold), NoiseThreshold, InvalidType.outOfBounds);
		if (Pattern == null) throw ExceptionHelper.Invalid(nameof(Pattern), InvalidType.isNull);
	}
}