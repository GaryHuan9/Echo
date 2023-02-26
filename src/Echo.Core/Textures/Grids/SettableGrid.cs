using System;
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
	/// <!-- https://github.com/dotnet/csharplang/issues/1568 -->
	public abstract void Set(Int2 position, in T value);

	public override SettableGrid<T> Crop(Int2 min, Int2 max) => new CropGrid(this, min, max);

	/// <summary>
	/// Fully empties the content of this <see cref="SettableGrid{T}"/>.
	/// </summary>
	public virtual void Clear() => ForEach(position => Set(position, default));

	class CropGrid : SettableGrid<T>
	{
		public CropGrid(SettableGrid<T> texture, Int2 min, Int2 max) : base(max - min)
		{
			if (!(Int2.Zero <= min) || !(max <= texture.size) || !(min < max)) throw new ArgumentException($"Invalid {nameof(min)} or {nameof(max)}: {min} and {max}.");

			source = texture;
			this.min = min;

			Wrapper = texture.Wrapper;
			Filter = texture.Filter;
		}

		readonly SettableGrid<T> source;
		readonly Int2 min;

		public override T this[Int2 position] => source[min + position];

		public override void Set(Int2 position, in T value) => source.Set(min + position, value);
	}
}