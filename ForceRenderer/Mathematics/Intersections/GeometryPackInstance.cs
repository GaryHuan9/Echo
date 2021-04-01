using CodeHelpers.Mathematics;

namespace ForceRenderer.Mathematics.Intersections
{
	public class GeometryPackInstance
	{
		readonly GeometryPack pack;

		readonly Float4x4 forwardTransform;
		readonly Float4x4 backwardTransform;

		public void GetIntersection(in Ray ray, ref Hit hit)
		{

		}

		public Ray TransformForward(in Ray ray)
		{
			Float3 origin = forwardTransform.MultiplyPoint(ray.origin);
			Float3 direction = forwardTransform.MultiplyDirection(ray.direction);

			return new Ray(origin, direction);
		}

		public CalculatedHit TransformBackward(in CalculatedHit hit)
		{
			Float3 position = backwardTransform.MultiplyPoint(hit.position);
			Float3 direction = backwardTransform.MultiplyDirection(hit.direction);
			Float3 normal = backwardTransform.MultiplyDirection(hit.normal).Normalized;

			return new CalculatedHit(position, direction, hit.distance, hit.material, normal, hit.texcoord);
		}
	}
}