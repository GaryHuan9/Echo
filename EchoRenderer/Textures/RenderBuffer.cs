using System;
using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Textures
{
	/// <summary>
	/// A regular <see cref="Texture2D"/> with albedo and normal auxiliary data.
	/// </summary>
	public class RenderBuffer : Texture2D
	{
		public RenderBuffer(Int2 size) : base(size)
		{
			albedos = new Float3[length];
			normals = new Float3[length];
		}

		public readonly Float3[] albedos;
		public readonly Float3[] normals;

		public Float3 GetAlbedo(Int2 position) => albedos[ToIndex(position)];
		public Float3 GetNormal(Int2 position) => normals[ToIndex(position)];

		public void SetAlbedo(Int2 position, Float3 value) => albedos[ToIndex(position)] = value;
		public void SetNormal(Int2 position, Float3 value) => normals[ToIndex(position)] = value;

		public Texture2D CreateAlbedoTexture() => CreateTexture(albedos);

		public Texture2D CreateNormalTexture() => CreateTexture(normals);

		public override void CopyFrom(Texture texture)
		{
			base.CopyFrom(texture);

			if (texture is not RenderBuffer buffer) return;

			Array.Copy(buffer.albedos, albedos, length);
			Array.Copy(buffer.normals, normals, length);
		}

		Texture2D CreateTexture(Float3[] data)
		{
			Texture2D texture = new Texture2D(size);
			texture.Foreach(SetPixel);

			return texture;

			void SetPixel(Int2 position)
			{
				ref Vector128<float> target = ref texture.GetPixel(position);
				target = Utilities.ToVector(Utilities.ToColor(data[ToIndex(position)]));
			}
		}

		//TODO: add serialization methods
	}
}