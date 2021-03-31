using CodeHelpers.Mathematics;

namespace ForceRenderer.Mathematics
{
	public readonly struct Hit
	{
		public Hit(float distance, GeometryPack pack, int token = default, Float2 uv = default)
		{
			this.distance = distance;
			this.pack = pack;

			this.token = token;
			this.uv = uv;
		}

		public readonly float distance;
		public readonly GeometryPack pack;

		public readonly Float2 uv;
		public readonly int token;
	}
}