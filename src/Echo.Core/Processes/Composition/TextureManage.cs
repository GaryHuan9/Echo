using System.Collections.Immutable;
using Echo.Core.Common.Compute.Async;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Processes.Composition;

/// <summary>
/// Provides a method of managing the <see cref="TextureGrid{T}"/> layers during compositing.
/// </summary>
/// <remarks>
/// This <see cref="ICompositeLayer"/> performs two actions: (1) insert and (2) copy. During insert,
/// <see cref="TextureGrid"/>s are added as new writable <see cref="SettableGrid{T}"/> layers. During
/// copy, existing readable <see cref="TextureGrid{T}"/>s of type <see cref="RGB128"/> are duplicated
/// into writeable <see cref="SettableGrid{T}"/> layers of type <see cref="RGB128"/>.
/// </remarks>
[EchoSourceUsable]
public record TextureManage : ICompositeLayer
{
	/// <summary>
	/// During insert, all the source <see cref="TextureGrid{T}"/> layers of type <see cref="RGB128"/> to insert.
	/// </summary>
	[EchoSourceUsable]
	public ImmutableArray<TextureGrid> InsertSources { get; init; } = ImmutableArray<TextureGrid>.Empty;

	/// <summary>
	/// During insert, the name of new destination <see cref="TextureGrid{T}"/> layers of type <see cref="RGB128"/> to create and insert as.
	/// </summary>
	[EchoSourceUsable]
	public ImmutableArray<string> InsertLayers { get; init; } = ImmutableArray<string>.Empty;

	/// <summary>
	/// During copy, the name of all source <see cref="TextureGrid{T}"/> layers of type <see cref="RGB128"/> to read from.
	/// </summary>
	[EchoSourceUsable]
	public ImmutableArray<string> CopySources { get; init; } = ImmutableArray<string>.Empty;

	/// <summary>
	/// During copy, the name of new destination <see cref="TextureGrid{T}"/> layers of type <see cref="RGB128"/> to create and copy to.
	/// </summary>
	[EchoSourceUsable]
	public ImmutableArray<string> CopyLayers { get; init; } = ImmutableArray<string>.Empty;

	/// <inheritdoc/>
	[EchoSourceUsable]
	public bool Enabled { get; init; } = true;

	public async ComputeTask ExecuteAsync(ICompositeContext context)
	{
		//Insert
		if (InsertSources.Length != InsertLayers.Length) throw new CompositeException($"Mismatch length between {nameof(InsertSources)} and {nameof(InsertLayers)}.");

		for (int i = 0; i < InsertSources.Length; i++)
		{
			if (InsertSources[i] is TextureGrid<RGB128> source) await CopyInto(context, source, InsertLayers[i]);
			else throw new CompositeException($"Cannot insert texture of type '{InsertSources[i].GetType()}'.");
		}

		//Copy
		if (CopySources.Length != CopyLayers.Length) throw new CompositeException($"Mismatch length between {nameof(CopySources)} and {nameof(CopyLayers)}.");

		for (int i = 0; i < CopySources.Length; i++)
		{
			var source = context.GetReadTexture<RGB128>(CopySources[i]);
			await CopyInto(context, source, CopyLayers[i]);
		}

		static ComputeTask CopyInto(ICompositeContext context, TextureGrid<RGB128> source, string name)
		{
			var target = new ArrayGrid<RGB128>(source.size);

			if (context.TryAddTexture(name, target)) return context.CopyAsync(source, target);
			throw new CompositeException($"Tried to add layer '{name}', but it already exists.");
		}
	}
}