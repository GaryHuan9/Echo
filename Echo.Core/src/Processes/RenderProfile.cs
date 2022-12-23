using System.Collections.Immutable;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Operation;
using Echo.Core.Processes.Composition;
using Echo.Core.Scenic.Hierarchies;

namespace Echo.Core.Processes;

public record RenderProfile
{
	public Scene Scene { get; init; }
	
	public Int2 Resolution { get; init; }

	public Int2 TileSize { get; init; } = new(16, 16);

	public ImmutableList<EvaluationProfile> EvaluationProfiles { get; private set; } = ImmutableList<EvaluationProfile>.Empty;

	public ImmutableList<ICompositionLayer> CompositionLayers { get; private set; } = ImmutableList<ICompositionLayer>.Empty;

	public void Add(EvaluationProfile profile) => EvaluationProfiles = EvaluationProfiles.Add(profile);

	public void Add(ICompositionLayer layer) => CompositionLayers = CompositionLayers.Add(layer);

	public void Validate()
	{
		
	}
}