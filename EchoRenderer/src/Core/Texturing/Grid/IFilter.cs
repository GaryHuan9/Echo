using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Coloring;

namespace EchoRenderer.Core.Texturing.Grid;

/// <summary>
/// Retrieves the pixel value of a <see cref="TextureGrid{T}"/> using a texture coordinate.
/// </summary>
public interface IFilter
{
	/// <summary>
	/// Returns the color of <paramref name="texture"/> at a texture coordinate <see cref="uv"/>.
	/// </summary>
	/// <param name="texture">The target texture to retrieve the color from.</param>
	/// <param name="uv">The texture coordinate. Must be between zero and one.</param>
	RGBA128 Convert<T>(TextureGrid<T> texture, Float2 uv) where T : IColor;
}

/// <summary>
/// A struct to temporarily change a <see cref="TextureGrid{T}.Filter"/>
/// and reverts the change after <see cref="Dispose"/> is invoked.
/// </summary>
public readonly struct ScopedFilter<T> : IDisposable where T : IColor
{
	public ScopedFilter(TextureGrid<T> texture, IFilter filter)
	{
		this.texture = texture;

		original = texture.Filter;
		texture.Filter = filter;
	}

	readonly TextureGrid<T> texture;
	readonly IFilter original;

	public void Dispose() => texture.Filter = original;
}

public static class Filters
{
	public static readonly IFilter point = new Point();
	public static readonly IFilter bilinear = new Bilinear();

	class Point : IFilter
	{
		/// <inheritdoc/>
		public RGBA128 Convert<T>(TextureGrid<T> texture, Float2 uv) where T : IColor
		{
			Int2 position = (uv * texture.size).Floored;
			return texture[position.Min(texture.oneLess)].ToRGBA128();
		}
	}

	class Bilinear : IFilter
	{
		/// <inheritdoc/>
		public RGBA128 Convert<T>(TextureGrid<T> texture, Float2 uv) where T : IColor
		{
			uv *= texture.size;

			Int2 upperRight = uv.Rounded;
			Int2 bottomLeft = upperRight - Int2.One;

			upperRight = upperRight.Min(texture.oneLess);
			bottomLeft = bottomLeft.Max(Int2.Zero);

			//Prefetch color data (273.6 ns => 194.6 ns)
			RGB128 y0x0 = texture[bottomLeft];
			RGB128 y0x1 = texture[new Int2(upperRight.X, bottomLeft.Y)];

			RGB128 y1x0 = texture[new Int2(bottomLeft.X, upperRight.Y)];
			RGB128 y1x1 = texture[upperRight];

			//Interpolate
			float timeX = InverseLerp(bottomLeft.X, upperRight.X, uv.X - 0.5f);
			float timeY = InverseLerp(bottomLeft.Y, upperRight.Y, uv.Y - 0.5f);

			Float4 y0 = Float4.Lerp(y0x0, y0x1, timeX);
			Float4 y1 = Float4.Lerp(y1x0, y1x1, timeX);

			return (RGB128)Float4.Lerp(y0, y1, timeY);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static float InverseLerp(int left, int right, float value)
			{
				if (left == right) return 0f;
				Assert.AreEqual(right, left + 1);
				return value - left; //Gap is always one
			}
		}
	}
}