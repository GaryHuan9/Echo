using System.Collections;
using System.Collections.Generic;
using CodeHelpers.Collections;
using CodeHelpers.Files;
using CodeHelpers.Mathematics;
using ForceRenderer.IO;

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

		public void ForEach(PixelPositionDelegate function)
		{
			for (int i = 0; i < pixels.Length; i++) function(ref pixels[i], ToPosition(i));
		}

		public IEnumerator<Float3> GetEnumerator() => ((IEnumerable<Float3>)pixels).GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public delegate void PixelDelegate(ref Float3 pixel);
		public delegate void PixelPositionDelegate(ref Float3 pixel, Int2 position);

		public void Save(string relativePath)
		{
			using FileWriter writer = new FileWriter(AssetsUtility.GetAssetsPath(relativePath));

			writer.Write(0);
			writer.Write(0);
			writer.Write(size);

			for (int i = 0; i < length; i++) writer.Write(pixels[i]);
		}

		public static RenderTexture Load(string relativePath, IWrapper wrapper = null, IFilter filter = null)
		{
			using FileReader reader = new FileReader(AssetsUtility.GetAssetsPath(relativePath));

			reader.ReadInt32();
			reader.ReadInt32();

			Int2 size = reader.ReadInt2();
			RenderTexture texture = new RenderTexture(size, wrapper, filter);
			Float3[] pixels = texture.pixels;

			for (int i = 0; i < texture.length; i++) pixels[i] = reader.ReadFloat3();

			return texture;
		}

		public void Write(FileWriter writer)
		{
			writer.Write(size);
			for (int i = 0; i < length; i++) writer.Write(pixels[i]);
		}

		public static RenderTexture Read(FileReader reader)
		{
			Int2 size = reader.ReadInt2();
			RenderTexture texture = new RenderTexture(size);

			for (int i = 0; i < texture.length; i++) texture.pixels[i] = reader.ReadFloat3();

			texture.SetReadonly();
			return texture;
		}
	}
}