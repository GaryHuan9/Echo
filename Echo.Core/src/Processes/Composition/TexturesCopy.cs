using System.Collections.Immutable;
using Echo.Core.Common.Compute.Async;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Processes.Composition;

/// <summary>
/// Duplicates a readable <see cref="TextureGrid{T}"/> into a writeable <see cref="SettableGrid{T}"/> with a different label.
/// </summary>
public record TexturesCopy : ICompositeLayer
{
	/// <summary>
	/// The labels of the source <see cref="TextureGrid{T}"/> layers of type <see cref="RGB128"/> to read from.
	/// </summary>
	public ImmutableArray<string> TargetLayers { get; init; } = ImmutableArray<string>.Empty;

	/// <summary>
	/// The labels of the destination <see cref="TextureGrid{T}"/> layers of type <see cref="RGB128"/> to create and copy to.
	/// </summary>
	public ImmutableArray<string> NewLabels { get; init; } = ImmutableArray<string>.Empty;

	public async ComputeTask ExecuteAsync(ICompositeContext context)
	{
		int length = TargetLayers.Length;
		if (length != NewLabels.Length) throw new CompositeException($"Mismatch length between {nameof(TargetLayers)} and {nameof(NewLabels)}.");

		var tasks = new ComputeTask[length];

		for (int i = 0; i < length; i++)
		{
			string label = NewLabels[i];

			var source = context.GetReadTexture<RGB128>(TargetLayers[i]);
			var target = new ArrayGrid<RGB128>(source.size);
			bool added = context.TryAddTexture(label, target);

			if (!added) throw new CompositeException($"Layer labeled '{label}' already exists.");
			tasks[i] = context.CopyAsync(source, target);
		}

		foreach (ComputeTask task in tasks) await task;
	}
}