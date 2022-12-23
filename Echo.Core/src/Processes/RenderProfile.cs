using System.Collections.Immutable;
using Echo.Core.Evaluation.Operation;
using Echo.Core.Processes.Composition;

namespace Echo.Core.Processes;

public record RenderProfile
{
	public ImmutableList<EvaluationProfile> EvaluationProfiles { get; private set; } = ImmutableList<EvaluationProfile>.Empty;
	
	public ImmutableList<ICompositionLayer> CompositionLayers { get; private set; } = ImmutableList<ICompositionLayer>.Empty;

	public void Add(EvaluationProfile profile) => EvaluationProfiles = EvaluationProfiles.Add(profile);

	public void Add(ICompositionLayer layer) => CompositionLayers = CompositionLayers.Add(layer);
}