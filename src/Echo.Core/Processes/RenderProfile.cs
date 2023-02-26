using System.Collections.Immutable;
using System.Numerics;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Processes.Composition;
using Echo.Core.Processes.Evaluation;
using Echo.Core.Scenic.Hierarchies;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Processes;

/// <summary>
/// A collection of parameters to configure and define a full render sequence.
/// </summary>
[EchoSourceUsable]
public record RenderProfile
{
	/// <summary>
	/// The <see cref="Scene"/> to render.
	/// </summary>
	[EchoSourceUsable]
	public Scene Scene { get; init; }

	/// <summary>
	/// The size of the destination <see cref="RenderTexture"/> to render to. 
	/// </summary>
	[EchoSourceUsable]
	public Int2 Resolution { get; init; } = new(960, 540);

	/// <summary>
	/// The size of each rendering tile. Each component must be a power of two.
	/// </summary>
	[EchoSourceUsable]
	public Int2 TileSize { get; init; } = new(16, 16);

	/// <summary>
	/// A list of <see cref="EvaluationProfile"/>s that define the series of evaluations to be performed on the <see cref="Scene"/>.
	/// </summary>
	/// <remarks>
	/// The chronological order of the evaluations is the same as their order in this list. This list must not be empty.
	/// </remarks>
	[EchoSourceUsable]
	public ImmutableArray<EvaluationProfile> EvaluationProfiles { get; init; }

	/// <summary>
	/// A list of <see cref="ICompositeLayer"/>s to be applied as post processing layers after the main evaluations are completed.
	/// </summary>
	/// <remarks>
	/// The chronological order of the processing steps is the same as their order in this list. This list can be empty.
	/// </remarks>
	[EchoSourceUsable]
	public ImmutableArray<ICompositeLayer> CompositionLayers { get; init; } = ImmutableArray<ICompositeLayer>.Empty;

	/// <summary>
	/// To be invoked to authenticate the validity of this <see cref="RenderProfile"/>.
	/// </summary>
	public void Validate()
	{
		if (Scene == null) throw ExceptionHelper.Invalid(nameof(Scene), InvalidType.isNull);
		if (!(Resolution > Int2.Zero)) throw ExceptionHelper.Invalid(nameof(Resolution), Resolution, InvalidType.outOfBounds);

		bool validTileSize = BitOperations.IsPow2(TileSize.X) && BitOperations.IsPow2(TileSize.Y);
		if (!validTileSize) throw ExceptionHelper.Invalid(nameof(TileSize), TileSize, InvalidType.outOfBounds);

		if (EvaluationProfiles.IsDefaultOrEmpty) throw ExceptionHelper.Invalid(nameof(EvaluationProfiles), InvalidType.countIsZero);
		if (CompositionLayers.IsDefault) throw ExceptionHelper.Invalid(nameof(CompositionLayers), InvalidType.isNull);

		foreach (EvaluationProfile profile in EvaluationProfiles) profile.Validate();
	}

	/// <summary>
	/// Schedules a new render onto a <see cref="Device"/> using this <see cref="RenderProfile"/>.
	/// </summary>
	/// <param name="device">The <see cref="Device"/> to schedule onto.</param>
	/// <returns>A <see cref="ScheduledRender"/> that represents the render.</returns>
	public ScheduledRender ScheduleTo(Device device) => ScheduledRender.Create(device, this);
}