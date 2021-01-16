using CodeHelpers.Mathematics;

namespace ForceRenderer.Mathematics
{
	public readonly struct Hit
	{
		public Hit(float distance, int token = default, Float2 uv = default)
		{
			this.distance = distance;
			this.token = token;
			this.uv = uv;
		}

		public readonly float distance;
		public readonly int token;
		public readonly Float2 uv;
	}
}
