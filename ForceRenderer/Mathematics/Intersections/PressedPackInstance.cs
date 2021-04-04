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

		/// <summary>
		/// Computes the AABB of all contents inside this instance based on its parent's coordinate system.
		/// This AABB does not necessary enclose the bvh root, only the enclosure of bvh content is guaranteed.
		/// NOTE: This property could be slow, so if performance issues arise try to memoize the result.
		/// </summary>
		public AxisAlignedBoundingBox AABB
		{
			get
			{
				// const int FetchIteration = 5; //How deep do we go into the bvh to get the vertices of its AABBs
				// Span<Float3> points = stackalloc Float3[(1 << FetchIteration) * 8];

				AxisAlignedBoundingBox root = pack.bvh.rootAABB;
				Span<Float3> points = stackalloc Float3[8];

				//TODO: test these two methods and try fetch deeper layers

				// Float3 center = backwardTransform.MultiplyPoint(root.center);
				// Float3 extend = backwardTransform.Absoluted.MultiplyDirection(root.extend);
				//
				// return new AxisAlignedBoundingBox(center, extend);

				points[0] = backwardTransform.MultiplyPoint(root.center + root.extend * new Float3(01f, 01f, 01f));
				points[1] = backwardTransform.MultiplyPoint(root.center + root.extend * new Float3(01f, 01f, -1f));
				points[2] = backwardTransform.MultiplyPoint(root.center + root.extend * new Float3(01f, -1f, 01f));
				points[3] = backwardTransform.MultiplyPoint(root.center + root.extend * new Float3(01f, -1f, -1f));
				points[4] = backwardTransform.MultiplyPoint(root.center + root.extend * new Float3(-1f, 01f, 01f));
				points[5] = backwardTransform.MultiplyPoint(root.center + root.extend * new Float3(-1f, 01f, -1f));
				points[6] = backwardTransform.MultiplyPoint(root.center + root.extend * new Float3(-1f, -1f, 01f));
				points[7] = backwardTransform.MultiplyPoint(root.center + root.extend * new Float3(-1f, -1f, -1f));

				return new AxisAlignedBoundingBox(points);
			}
		}

		public readonly PressedPack pack;

		readonly Float4x4 forwardTransform;  //The parent to local transform matrix
		readonly Float4x4 backwardTransform; //The local to parent transform matrix

		readonly float forwardScale;  //The parent to local scale multiplier
		readonly float backwardScale; //The local to parent scale multiplier

		public void GetIntersection(in Ray ray, ref Hit hit)
		{
			hit.distance *= forwardScale;
			float distance = hit.distance;

			//Gets intersection from bvh, calculation done in local space
			pack.bvh.GetIntersection(TransformForward(ray), ref hit);

			bool skip = distance == hit.distance; //Must use exact comparison to check for modification
			hit.distance *= backwardScale;        //Compare before multiplication to avoid float math issues

			//If the distance did not change, it means no intersection made thus we can skip
			if (skip) return;

			//If we hit an actual geometry, the pack instance will be null
			if (hit.instance == null)
			{
				hit.instance = this;
				pack.GetNormal(ref hit);
			}

			//We have to transform the normal from local to parent space
			hit.normal = backwardTransform.MultiplyDirection(hit.normal) * forwardScale;
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

			return new Ray(origin, direction * backwardScale);
		}
	}
}