using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Textures.Grids;

/// <summary>
/// A <see cref="TextureGrid{T}"/> that is assignable (ie. can be set/modified pixel by pixel).
/// </summary>
public abstract class SettableGrid<T> : TextureGrid<T> where T : unmanaged, IColor<T>
{
	protected SettableGrid(Int2 size) : base(size) { }

	/// <summary>
	/// Sets the pixel value of type <see cref="T"/> of this <see cref="TextureGrid{T}"/> at a <paramref name="position"/>.
	/// </summary>
	/// <param name="position">The integral pixel position to get the value from. This <see cref="Int2"/> must 
	/// be between <see cref="Int2.Zero"/> (inclusive) and <see cref="TextureGrid{T}.size"/> (exclusive).</param>
	/// <param name="value">The value of type <see cref="T"/> to set.</param>
	/// <remarks>The reason that this is a method but not an indexer is because of C#'s
	/// (pathetic) inability to extend abstract indexers in derived classes.</remarks>
	/// <!--https://github.com/dotnet/csharplang/issues/1568-->
	public abstract void Set(Int2 position, in T value);

	/// <summary>
	/// Copies as much data from <paramref name="texture"/> to this <see cref="Texture"/>.
	/// </summary>
	public virtual void CopyFrom(Texture texture) => ForEach
	(
		texture is TextureGrid<T> grid && grid.size == size ?
			position => Set(position, grid[position]) :
			position => Set(position, texture[ToUV(position)].As<T>())
	);

	/// <summary>
	/// Fully empties the content of this <see cref="SettableGrid{T}"/>.
	/// </summary>
	public virtual void Clear() => ForEach(position => Set(position, default));
}