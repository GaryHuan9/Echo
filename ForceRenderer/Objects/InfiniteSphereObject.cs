using CodeHelpers.Vectors;

namespace ForceRenderer.Objects
{
	public class InfiniteSphereObject : SceneObject
	{
		public InfiniteSphereObject(Float3 gap, float radius)
		{
			Gap = gap;
			Radius = radius;
		}

		public float Radius { get; private set; }

		Float3 gap;
		Float3 half;

		public Float3 Gap
		{
			get => gap;
			set
			{
				gap = value;
				half = value / 2f;
			}
		}

		public override float SignedDistanceRaw(Float3 point)
		{
			Float3 q = Mod(point + half, gap) - half;
			return q.Magnitude - Radius;
		}

		static Float3 Mod(Float3 value, Float3 other) => value - other * (value / other).FlooredFloat;
	}
}