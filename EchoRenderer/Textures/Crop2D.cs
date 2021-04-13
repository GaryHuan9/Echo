using System.Runtime.Intrinsics;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Textures
{
	public class Crop2D : Texture
	{
		/// <summary>
		/// Creates the rectangular cropped reference of <paramref name="source"/>.
		/// <paramref name="cornerMin"/> is inclusive and <paramref name="cornerMax"/> is exclusive.
		/// </summary>
		public Crop2D(Texture source, Int2 cornerMin, Int2 cornerMax) : base(cornerMax - cornerMin)
		{
			this.source = source;
			this.cornerMin = cornerMin;

			Assert.IsTrue(cornerMax > this.cornerMin);
		}

		readonly Texture source;
		readonly Int2 cornerMin;

		public override ref Vector128<float> this[int index] => ref GetPixelRaw(ToPosition(index));

		public override ref Vector128<float> GetPixel(Int2 position) => ref GetPixelRaw(Wrapper.Convert(this, position));

		ref Vector128<float> GetPixelRaw(Int2 position)
		{
			int index = source.ToIndex(position + cornerMin);
			return ref source[index];
		}
	}
}