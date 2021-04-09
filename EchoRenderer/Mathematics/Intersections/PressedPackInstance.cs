using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Objects;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Mathematics.Intersections
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
				const int FetchDepth = 5; //How deep do we go into the bvh to get the AABB of the nodes
				Span<AxisAlignedBoundingBox> aabbs = stackalloc AxisAlignedBoundingBox[1 << (FetchDepth - 1)];

				int count = pack.bvh.FillAABB(FetchDepth, aabbs);
				Float4x4 absoluteTransform = backwardTransform.Absoluted;

				Float3 min = Float3.positiveInfinity;
				Float3 max = Float3.negativeInfinity;

				//Find a small AABB by encapsulating children nodes of the bvh instead of the full bvh
				for (int i = 0; i < count; i++)
				{
					ref readonly var aabb = ref aabbs[i];

					Float3 center = backwardTransform.MultiplyPoint(aabb.center);
					Float3 extend = absoluteTransform.MultiplyDirection(aabb.extend);

					min = min.Min(center - extend);
					max = max.Max(center + extend);
				}

				Float3 newExtend = (max - min) / 2f;
				return new AxisAlignedBoundingBox(min + newExtend, newExtend);
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

		/// <summary>
		/// Returns the material from the indicated <paramref name="token"/>.
		/// The method might maps
		/// </summary>
		public Material GetMaterial(int token)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Transforms <paramref name="ray"/> from parent to local space and returns the new ray.
		/// </summary>
		Ray TransformForward(in Ray ray)
		{
			Float3 origin = forwardTransform.MultiplyPoint(ray.origin);
			Float3 direction = forwardTransform.MultiplyDirection(ray.direction);

			return new Ray(origin, direction * backwardScale);
		}
	}
}