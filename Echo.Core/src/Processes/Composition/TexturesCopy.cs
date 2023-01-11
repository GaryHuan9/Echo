using System.Collections.Immutable;
using Echo.Core.Common.Compute.Async;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Processes.Composition;

/// <summary>
/// Duplicates a readable <see cref="TextureGrid{T}"/> of type <see cref="RGB128"/>
/// into a writeable <see cref="SettableGrid{T}"/> with a different label.
/// </summary>
[EchoSourceUsable]
public record TexturesCopy : ICompositeLayer
{
	/// <summary>
	/// The labels of the source <see cref="TextureGrid{T}"/> layers of type <see cref="RGB128"/> to read from.
	/// </summary>
	[EchoSourceUsable]
	public ImmutableArray<string> Sources { get; init; } = ImmutableArray<string>.Empty;

	/// <summary>
	/// The labels of the destination <see cref="TextureGrid{T}"/> layers of type <see cref="RGB128"/> to create and copy to.
	/// </summary>
	[EchoSourceUsable]
	public ImmutableArray<string> Targets { get; init; } = ImmutableArray<string>.Empty;

	public async ComputeTask ExecuteAsync(ICompositeContext context)
	{
		int length = Sources.Length;
		if (length != Targets.Length) throw new CompositeException($"Mismatch length between {nameof(Sources)} and {nameof(Targets)}.");

		var tasks = new ComputeTask[length];

		for (int i = 0; i < length; i++)
		{
			string label = Targets[i];

			var source = context.GetReadTexture<RGB128>(Sources[i]);
			var target = new ArrayGrid<RGB128>(source.size);
			bool added = context.TryAddTexture(label, target);

			if (!added) throw new CompositeException($"Layer labeled '{label}' already exists.");
			tasks[i] = context.CopyAsync(source, target);
		}

		foreach (ComputeTask task in tasks) await task;
	}
}