﻿using Echo.Core.Common.Diagnostics;
using Echo.Core.Evaluation.Evaluators;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Processes.Evaluation;

/// <summary>
/// A collection of parameters to configure and define an <see cref="EvaluationOperation"/>.
/// </summary>
[EchoSourceUsable]
public record EvaluationProfile
{
	/// <summary>
	/// The fundamental evaluation method used.
	/// </summary>
	[EchoSourceUsable]
	public Evaluator Evaluator { get; init; }

	/// <summary>
	/// The name of the destination layer in the <see cref="RenderTexture"/> to write to.
	/// </summary>
	[EchoSourceUsable]
	public string LayerName { get; init; }

	/// <summary>
	/// The <see cref="ContinuousDistribution"/> used for this evaluation.
	/// </summary>
	/// <remarks>
	/// The <see cref="ContinuousDistribution.Extend"/> value specifies the precise number of samples used per epoch.
	/// This instance of the <see cref="ContinuousDistribution"/> should not be directly used; it is to only provide
	/// a template to clone new distributions for each worker using the C# record `with` syntax.
	/// </remarks>
	[EchoSourceUsable]
	public ContinuousDistribution Distribution { get; init; } = new StratifiedDistribution();

	/// <summary>
	/// The minimum number of epochs that must be performed before adaptive sampling begins.
	/// </summary>
	[EchoSourceUsable]
	public int MinEpoch { get; init; } = 1;

	/// <summary>
	/// The maximum possible number of epochs that can be performed.
	/// </summary>
	[EchoSourceUsable]
	public int MaxEpoch { get; init; } = 20;

	/// <summary>
	/// Evaluation is completed after noise is under this threshold.
	/// </summary>
	[EchoSourceUsable]
	public float NoiseThreshold { get; init; } = 0.045f;

	/// <summary>
	/// The <see cref="ITilePattern"/> used to determine the sequence of the tiles.
	/// </summary>
	[EchoSourceUsable]
	public ITilePattern Pattern { get; init; } = new HilbertCurvePattern();

	/// <summary>
	/// To be invoked to authenticate the validity of this <see cref="EvaluationProfile"/>.
	/// </summary>
	public void Validate()
	{
		if (Evaluator == null) throw ExceptionHelper.Invalid(nameof(Evaluator), InvalidType.isNull);
		if (string.IsNullOrEmpty(LayerName)) throw ExceptionHelper.Invalid(nameof(LayerName), InvalidType.isNull);
		if (Distribution == null) throw ExceptionHelper.Invalid(nameof(Distribution), InvalidType.isNull);

		if (MinEpoch <= 0) throw ExceptionHelper.Invalid(nameof(MinEpoch), MinEpoch, InvalidType.outOfBounds);
		if (MaxEpoch < MinEpoch) throw ExceptionHelper.Invalid(nameof(MaxEpoch), MaxEpoch, InvalidType.outOfBounds);
		if (NoiseThreshold < 0f) throw ExceptionHelper.Invalid(nameof(NoiseThreshold), NoiseThreshold, InvalidType.outOfBounds);
		if (Pattern == null) throw ExceptionHelper.Invalid(nameof(Pattern), InvalidType.isNull);
	}
}