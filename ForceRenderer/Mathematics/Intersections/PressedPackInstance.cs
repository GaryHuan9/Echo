using CodeHelpers.Mathematics;

namespace ForceRenderer.Mathematics.Intersections
{
	public class PressedPackInstance
	{
		readonly PressedPack pack;

		readonly Float4x4 forwardTransform; //This transform only transforms from the parent space to local space

		readonly Float4x4 backwardTransform; //These two properties are transforms from local space to world space directly,
		readonly float backwardScaling;      //since our backward transform does not recurse up the instance tree

		public void GetIntersection(in Ray ray, ref Hit hit)
		{
			float distance = hit.distance;

			pack.bvh.GetIntersection(TransformForward(ray), ref hit);
			if (Scalars.AlmostEquals(distance, hit.distance)) return;

			hit.instance ??= this;
		}

		public int GetIntersectionCost(in Ray ray, ref float distance) => pack.bvh.GetIntersectionCost(TransformForward(ray), ref distance);

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