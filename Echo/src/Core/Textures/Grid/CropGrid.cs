using CodeHelpers;
using CodeHelpers.Packed;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Textures.Grid;

/// <summary>
/// A rectangular cropped view into a <see cref="TextureGrid{T}"/>.
/// </summary>
public class CropGrid<T> : SettableGrid<T> where T : unmanaged, IColor<T>
{
	/// <summary>
	/// Constructs a <see cref="CropGrid{T}"/> from <paramref name="min"/> (inclusive) to <paramref name="max"/> (exclusive).
	/// </summary>
	public CropGrid(TextureGrid<T> texture, Int2 min, Int2 max) : base(max - min)
	{
		if (!(max > min)) throw ExceptionHelper.Invalid(nameof(max), max, InvalidType.outOfBounds);

		settable = texture as SettableGrid<T>;
		source = texture;
		this.min = min;

		Wrapper = texture.Wrapper;
		Filter = texture.Filter;
	}

	readonly SettableGrid<T> settable;
	readonly TextureGrid<T> source;
	readonly Int2 min;

	public override T this[Int2 position] => source[min + position];

	public override void Set(Int2 position, in T value) => settable?.Set(min + position, value);
}

public static class CropGridExtensions
{
	/// <summary>
	/// Crops <paramref name="texture"/> from <see cref="min"/> (inclusive) to
	/// the maximum corner (top right) by creating a <see cref="CropGrid{T}"/>.
	/// </summary>
	public static CropGrid<T> Crop<T>(this TextureGrid<T> texture, Int2 min) where T : unmanaged, IColor<T> => new(texture, min, texture.size);

	/// <summary>
	/// Crops <paramref name="texture"/> from <see cref="min"/> (inclusive) to the
	/// <paramref name="max"/> (exclusive) by creating a <see cref="CropGrid{T}"/>.
	/// </summary>
	public static CropGrid<T> Crop<T>(this TextureGrid<T> texture, Int2 min, Int2 max) where T : unmanaged, IColor<T> => new(texture, min, max);
}