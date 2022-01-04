using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Textures.Grid
{
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
			public Vector128<float> Convert(TextureGrid texture, Float2 uv)
			{
				Int2 position = (uv * texture.size).Floored;
				return texture[position.Min(texture.oneLess)];
			}
		}

		class Bilinear : IFilter
		{
			//If the performance of this bilinear filter is not fast enough anymore, we could always move to a 'native'
			//implementation for TextureGrid and allow derived class to provide customized implementations with virtual methods

			public Vector128<float> Convert(TextureGrid texture, Float2 uv)
			{
				uv *= texture.size;

				Int2 upperRight = uv.Rounded;
				Int2 bottomLeft = upperRight - Int2.one;

				upperRight = upperRight.Min(texture.oneLess);
				bottomLeft = bottomLeft.Max(Int2.zero);

				//Prefetch color data (273.6 ns => 194.6 ns)
				Vector128<float> y0x0 = texture[bottomLeft];
				Vector128<float> y0x1 = texture[new Int2(upperRight.x, bottomLeft.y)];

				Vector128<float> y1x0 = texture[new Int2(bottomLeft.x, upperRight.y)];
				Vector128<float> y1x1 = texture[upperRight];

				//Interpolate
				Vector128<float> timeX = Vector128.Create(InverseLerp(bottomLeft.x, upperRight.x, uv.x - 0.5f));
				Vector128<float> timeY = Vector128.Create(InverseLerp(bottomLeft.y, upperRight.y, uv.y - 0.5f));

				Vector128<float> y0 = Utilities.Lerp(y0x0, y0x1, timeX);
				Vector128<float> y1 = Utilities.Lerp(y1x0, y1x1, timeX);

				return Utilities.Lerp(y0, y1, timeY);

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
}