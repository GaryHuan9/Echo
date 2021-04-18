using CodeHelpers.Mathematics;

namespace EchoRenderer.Textures
{
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

		//TODO: add serialization methods
	}
}