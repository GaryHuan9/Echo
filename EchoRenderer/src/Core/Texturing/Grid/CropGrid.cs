global using CropGrid = EchoRenderer.Core.Texturing.Grid.CropGrid<EchoRenderer.Common.Coloring.RGB128>;
//
using CodeHelpers;
using CodeHelpers.Packed;
using EchoRenderer.Common.Coloring;

namespace EchoRenderer.Core.Texturing.Grid;

public class CropGrid<T> : TextureGrid<T> where T : IColor<T>
{
	/// <summary>
	/// Creates the rectangular cropped reference of <paramref name="source"/>.
	/// <paramref name="min"/> is inclusive and <paramref name="max"/> is exclusive.
	/// </summary>
	public CropGrid(TextureGrid<T> source, Int2 min, Int2 max) : base(max - min)
	{
		if (!(max > min)) throw ExceptionHelper.Invalid(nameof(max), max, InvalidType.outOfBounds);

		this.source = source;
		this.min = min;

		Wrapper = source.Wrapper;
		Filter = source.Filter;
	}

	readonly TextureGrid<T> source;
	readonly Int2 min;

	public override T this[Int2 position]
	{
		get => source[min + position];
		set => source[min + position] = value;
	}
}