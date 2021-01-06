using System.Collections;
using System.Collections.Generic;
using CodeHelpers.Collections;
using CodeHelpers.Mathematics;

namespace ForceRenderer.Textures
{
	/// <summary>
	/// A large memory footprint texture that is meant to be rendered on. Preserves high precision colors.
	/// Uses 3 single-precision floats per channel, for a total of 3 channels across the RGB colors.
	/// </summary>
	public class RenderTexture : Texture, IEnumerable<Float3>
	{
		public RenderTexture(Int2 size, IWrapper wrapper = null, IFilter filter = null) : base(size, wrapper, filter) => pixels = new Float3[size.Product];

		public RenderTexture(Texture source, IWrapper wrapper = null, IFilter filter = null) : this(source.size, wrapper, filter)
		{
			if (source is RenderTexture texture)
			{
				for (int i = 0; i < length; i++) pixels[i] = texture.pixels[i];
			}
			else CopyFrom(source);
		}

		readonly Float3[] pixels;

		public override Float3 this[int index]
		{
			get => pixels[index];
			set
			{
				CheckReadonly();
				pixels[index] = value;
			}
		}

		public void ForEach(PixelDelegate function)
		{
			for (int i = 0; i < pixels.Length; i++) function(ref pixels[i]);
		}

		public IEnumerator<Float3> GetEnumerator() => ((IEnumerable<Float3>)pixels).GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public delegate void PixelDelegate(ref Float3 pixel);
	}
}