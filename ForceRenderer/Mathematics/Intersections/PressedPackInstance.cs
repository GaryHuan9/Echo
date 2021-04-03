using System;
using CodeHelpers.Mathematics;
using ForceRenderer.Objects;
using ForceRenderer.Objects.Scenes;

namespace ForceRenderer.Mathematics.Intersections
{
	public class PressedPackInstance
	{
		public PressedPackInstance(ObjectPackInstance instance, ScenePresser presser)
		{
			pack = presser.GetPressedPack(instance.ObjectPack);

			backwardTransform = instance.LocalToWorld;
			forwardTransform = instance.WorldToLocal;

			Float3 scale = instance.Scale;

			if (scale.Sorted == scale.SortedReversed)
			{
				backwardScale = scale.Average;
				forwardScale = 1f / backwardScale;
			}
			else throw new Exception($"{nameof(ObjectPackInstance)} does not support none uniform scaling! '{scale}'");
		}

		public AxisAlignedBoundingBox AABB
		{
			get
			{
				AxisAlignedBoundingBox root = pack.bvh.rootAABB;

				Float3 center = backwardTransform.MultiplyPoint(root.center);
				Float3 extend = backwardTransform.MultiplyDirection(root.extend);

				return new AxisAlignedBoundingBox(center, extend.Absoluted);
			}
		}

		public readonly PressedPack pack;

		readonly Float4x4 forwardTransform;  //The parent to local transform matrix
		readonly Float4x4 backwardTransform; //The local to parent transform matrix

		readonly float forwardScale;  //The parent to local scale multiplier
		readonly float backwardScale; //The local to parent scale multiplier

		public void GetIntersection(in Ray ray, ref Hit hit)
		{
			float distance = hit.distance;
			hit.distance *= forwardScale;

			//Gets intersection from bvh, calculation done in local space
			pack.bvh.GetIntersection(TransformForward(ray), ref hit);

			hit.distance *= backwardScale;

			//If the distance did not change, it means no intersection made thus we can skip
			if (Scalars.AlmostEquals(distance, hit.distance)) return;

			//If we hit an actual geometry, the pack instance will be null
			if (hit.instance == null)
			{
				hit.instance = this;
				pack.GetNormal(ref hit);
			}

			//We have to transform the normal from local to parent space
			hit.normal = backwardTransform.MultiplyDirection(hit.normal);
		}

		public int GetIntersectionCost(in Ray ray, ref float distance)
		{
			//Forward transform distance to local space
			distance *= forwardScale;

			//Gets intersection cost from bvh, calculation done in local space
			int cost = pack.bvh.GetIntersectionCost(TransformForward(ray), ref distance);

			//Transforms distance back to parent space
			distance *= backwardScale;
			return cost;
		}

		Ray TransformForward(in Ray ray)
		{
			Float3 origin = forwardTransform.MultiplyPoint(ray.origin);
			Float3 direction = forwardTransform.MultiplyDirection(ray.direction);

			return new Ray(origin, direction);
		}
	}
}