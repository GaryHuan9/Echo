using CodeHelpers.Vectors;

namespace ForceRenderer.Objects.SceneObjects
{
	public class PlaneObject : SceneObject
	{
		public override float GetSignedDistanceRaw(Float3 point) => point.y;
		public override Float3 GetNormalRaw(Float3 point) => Float3.up;
	}
}