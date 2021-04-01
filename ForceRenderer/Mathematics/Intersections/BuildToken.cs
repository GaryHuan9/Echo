using CodeHelpers.Mathematics;

namespace ForceRenderer.Mathematics.Intersections
{
	public readonly struct BuildToken
	{
		public BuildToken(int token, Float3 min, Float3 max)
		{
			this.token = token;
			this.min = min;
			this.max = max;
		}

		public readonly int token;

		public readonly Float3 min;
		public readonly Float3 max;

		public float Area
		{
			get
			{
				Float3 size = max - min;
				return size.x * size.y + size.x * size.z + size.y * size.z;
			}
		}

		public AxisAlignedBoundingBox AABB
		{
			get
			{
				Float3 extend = (max - min) / 2f;
				return new AxisAlignedBoundingBox(min + extend, extend);
			}
		}
	}
}