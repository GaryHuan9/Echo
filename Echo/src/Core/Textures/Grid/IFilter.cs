using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Packed;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Textures.Grid;

/// <summary>
/// Retrieves the pixel value of a <see cref="TextureGrid{T}"/> using a texture coordinate.
/// </summary>
public interface IFilter
{
	public static readonly IFilter point = new Point();
	public static readonly IFilter bilinear = new Bilinear();

	/// <summary>
	/// Returns the color of <paramref name="texture"/> at a texture coordinate <see cref="uv"/>.
	/// </summary>
	/// <param name="texture">The target texture to retrieve the color from.</param>
	/// <param name="uv">The texture coordinate. Must be between zero and one.</param>
	RGBA128 Evaluate<T>(TextureGrid<T> texture, Float2 uv) where T : unmanaged, IColor<T>;

	class Point : IFilter
	{
		/// <inheritdoc/>
		public RGBA128 Evaluate<T>(TextureGrid<T> texture, Float2 uv) where T : unmanaged, IColor<T>
		{
			Int2 position = texture.ToPosition(uv);
			texture.Wrapper.Wrap(texture, ref position);
			return texture[position].ToRGBA128();
		}
	}

	class Bilinear : IFilter
	{
		/// <inheritdoc/>
		public RGBA128 Evaluate<T>(TextureGrid<T> texture, Float2 uv) where T : unmanaged, IColor<T>
		{
			//Find x and y
			Float2 scaled = uv * texture.size;

			Vector128<int> x = Sse2.ConvertToVector128Int32(Vector128.Create(scaled.X));
			Vector128<int> y = Sse2.ConvertToVector128Int32(Vector128.Create(scaled.Y));

			x = Sse2.Subtract(x, Vector128.Create(1, 0, 1, 0));
			y = Sse2.Subtract(y, Vector128.Create(1, 1, 0, 0));

			//Wrap and shuffle
			int minX = x.GetElement(0);
			int minY = y.GetElement(0);

			texture.Wrapper.Wrap(texture, ref x, ref y);

			//OPTIMIZE shuffle x and y so we only use half of the GetElement calls

			//Fetch color data
			RGBA128 y0x0 = texture[new Int2(x.GetElement(0), y.GetElement(0))].ToRGBA128();
			RGBA128 y0x1 = texture[new Int2(x.GetElement(1), y.GetElement(1))].ToRGBA128();

			RGBA128 y1x0 = texture[new Int2(x.GetElement(2), y.GetElement(2))].ToRGBA128();
			RGBA128 y1x1 = texture[new Int2(x.GetElement(3), y.GetElement(3))].ToRGBA128();

			//Interpolate
			float timeX = scaled.X - 0.5f - minX;
			float timeY = scaled.Y - 0.5f - minY;

			Float4 y0 = Float4.Lerp(y0x0, y0x1, timeX);
			Float4 y1 = Float4.Lerp(y1x0, y1x1, timeX);

			return (RGBA128)Float4.Lerp(y0, y1, timeY);
		}
	}
}