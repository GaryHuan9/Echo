using CodeHelpers.Vectors;

namespace ForceRenderer.Objects
{
	public class SphereObject : SceneObject
	{
		public SphereObject(float radius) => Radius = radius;

		public float Radius { get; set; }

		public override float GetSignedDistanceRaw(Float3 point) => point.Magnitude - Radius;
		public override Float3 GetNormalRaw(Float3 point) => point.Normalized;
	}
}