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

	public ImmutableArray<EvaluationProfile> EvaluationProfiles { get; init; } = ImmutableArray<EvaluationProfile>.Empty;

	public ImmutableArray<ICompositionLayer> CompositionLayers { get; init; } = ImmutableArray<ICompositionLayer>.Empty;

	public void Validate() { }
}