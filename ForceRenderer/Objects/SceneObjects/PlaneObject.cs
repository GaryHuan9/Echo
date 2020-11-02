using CodeHelpers.Vectors;
using ForceRenderer.Mathematics;
using ForceRenderer.Renderers;

namespace ForceRenderer.Objects.SceneObjects
{
	public class PlaneObject : SceneObject
	{
		public PlaneObject(Material material) : base(material) { }

		public override float GetRawIntersection(in Ray ray)
		{
			float distance = -ray.origin.y / ray.direction.y;
			return distance < 0f ? float.PositiveInfinity : distance;
		}

		public override Float3 GetRawNormal(Float3 point) => Float3.up;
	}
}