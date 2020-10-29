using CodeHelpers.Vectors;

namespace ForceRenderer
{
	public class SphereObject : SceneObject
	{
		public SphereObject(float radius) => Radius = radius;

		public float Radius { get; set; }

		public override float SignedDistance(Float3 point) => (point - Position).Magnitude - Radius;
	}
}