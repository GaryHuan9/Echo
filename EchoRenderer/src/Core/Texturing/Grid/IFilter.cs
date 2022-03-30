using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics;

namespace EchoRenderer.Core.Texturing.Grid;

/// <summary>
/// Retrieves the pixel of a <see cref="TextureGrid"/> using a texture coordinate.
/// </summary>
public interface IFilter
{
	/// <summary>
	/// Returns the color of <paramref name="texture"/> at a texture coordinate <see cref="uv"/>.
	/// </summary>
	/// <param name="texture">The target texture to retrieve the color from.</param>
	/// <param name="uv">The texture coordinate. Must be between zero and one.</param>
	Vector128<float> Convert(TextureGrid texture, Float2 uv);
}

/// <summary>
/// A struct to temporarily change a <see cref="TextureGrid.Filter"/>
/// and reverts the change after <see cref="Dispose"/> is invoked.
/// </summary>
public readonly struct ScopedFilter : IDisposable
{
	public ScopedFilter(TextureGrid texture, IFilter filter)
	{
		this.texture = texture;

		original = texture.Filter;
		texture.Filter = filter;
	}

	readonly TextureGrid texture;
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
		public Vector128<float> Convert(TextureGrid texture, Float2 uv)
		{
			Int2 position = (uv * texture.size).Floored;
			return texture[position.Min(texture.oneLess)];
		}
	}

	class Bilinear : IFilter
	{
		// If the performance of this bilinear filter is not fast enough anymore, we could always move to a more
		// 'native' approach by allowing derived class to provide customized implementations with virtual methods

		/// <inheritdoc/>
		public Vector128<float> Convert(TextureGrid texture, Float2 uv)
		{
			uv *= texture.size;

			Int2 upperRight = uv.Rounded;
			Int2 bottomLeft = upperRight - Int2.One;

			upperRight = upperRight.Min(texture.oneLess);
			bottomLeft = bottomLeft.Max(Int2.Zero);

			//Prefetch color data (273.6 ns => 194.6 ns)
			Vector128<float> y0x0 = texture[bottomLeft];
			Vector128<float> y0x1 = texture[new Int2(upperRight.X, bottomLeft.Y)];

			Vector128<float> y1x0 = texture[new Int2(bottomLeft.X, upperRight.Y)];
			Vector128<float> y1x1 = texture[upperRight];

			//Interpolate
			Vector128<float> timeX = Vector128.Create(InverseLerp(bottomLeft.X, upperRight.X, uv.X - 0.5f));
			Vector128<float> timeY = Vector128.Create(InverseLerp(bottomLeft.Y, upperRight.Y, uv.Y - 0.5f));

			Vector128<float> y0 = PackedMath.Lerp(y0x0, y0x1, timeX);
			Vector128<float> y1 = PackedMath.Lerp(y1x0, y1x1, timeX);

			return PackedMath.Lerp(y0, y1, timeY);

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