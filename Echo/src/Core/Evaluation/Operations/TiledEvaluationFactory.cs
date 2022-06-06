using System;
using System.Collections.Immutable;
using CodeHelpers;
using CodeHelpers.Packed;
using Echo.Common.Compute;
using Echo.Common.Memory;
using Echo.Core.Evaluation.Distributions.Continuous;

namespace Echo.Core.Evaluation.Operations;

/// <summary>
/// An implementation of <see cref="IOperationFactory"/> for tiled evaluation.
/// </summary>
public class TiledEvaluationFactory : IOperationFactory
{
	//cache contexts to reuse them
	TiledEvaluationOperation.Context[] contexts;
	NotNull<TiledEvaluationProfile> _nextProfile;

	/// <summary>
	/// The next <see cref="TiledEvaluationProfile"/> to use.
	/// </summary>
	/// <remarks>Cannot be null.</remarks>
	public TiledEvaluationProfile NextProfile
	{
		get => _nextProfile;
		set => _nextProfile = value;
	}

	/// <inheritdoc />
	public Operation CreateOperation(ImmutableArray<IWorker> workers)
	{
		//Validate profile
		var profile = NextProfile ?? throw ExceptionHelper.Invalid(nameof(NextProfile), InvalidType.isNull);
		profile.Validate();

		//Create tile sequence and contexts
		Int2 size = profile.Buffer.size.CeiledDivide(profile.TileSize);
		Int2[] tileSequence = profile.Pattern.CreateSequence(size);

		CreateContexts(profile, workers.Length);

		return new TiledEvaluationOperation
		(
			workers, profile,
			tileSequence.ToImmutableArray(),
			contexts.ToImmutableArray()
		);
	}

	void CreateContexts(TiledEvaluationProfile profile, int population)
	{
		if (contexts == null || contexts.Length < population) Array.Resize(ref contexts, population);

		ContinuousDistribution source = profile.Distribution;

		foreach (ref TiledEvaluationOperation.Context context in contexts.AsSpan(0, population))
		{
			if (context.Distribution != source) context = context with { Distribution = source with { } };
			if (context.Allocator == null) context = context with { Allocator = new Allocator() };
		}
	}
}