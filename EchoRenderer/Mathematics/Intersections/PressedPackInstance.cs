using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Mathematics;
using EchoRenderer.Objects;
using EchoRenderer.Objects.Scenes;

namespace EchoRenderer.Mathematics.Intersections
{
	public class PressedPackInstance
	{
		public PressedPackInstance(ScenePresser presser, ObjectPackInstance instance) : this(presser, instance.ObjectPack, instance.Mapper)
		{
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

		public PressedPackInstance(ScenePresser presser, Scene scene) : this(presser, scene, null)
		{
			forwardTransform = Float4x4.identity;
			backwardTransform = Float4x4.identity;

			forwardScale = 1f;
			backwardScale = 1f;
		}

		PressedPackInstance(ScenePresser presser, ObjectPack pack, MaterialMapper mapper)
		{
			id = presser.RegisterPressedPackInstance(this);
			this.pack = presser.GetPressedPack(pack);
			this.mapper = presser.materials.GetMapper(mapper);
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

					Float3 center = backwardTransform.MultiplyPoint(aabb.Center);
					Float3 extend = absoluteTransform.MultiplyDirection(aabb.Extend);

					min = min.Min(center - extend);
					max = max.Max(center + extend);
				}

				return new AxisAlignedBoundingBox(min, max);
			}
		}

		public readonly uint id;
		public readonly PressedPack pack;
		public readonly MaterialPresser.Mapper mapper;

		readonly Float4x4 forwardTransform;  //The parent to local transform matrix
		readonly Float4x4 backwardTransform; //The local to parent transform matrix

		readonly float forwardScale;  //The parent to local scale multiplier
		readonly float backwardScale; //The local to parent scale multiplier

		public void GetIntersection(ref HitQuery query)
		{
			query.distance *= forwardScale;

			var oldRay = query.ray;
			var oldInstance = query.instance;
			var oldDistance = query.distance;

			query.ray = TransformForward(query.ray);
			query.instance = this;

			//Gets intersection from bvh, calculation done in local space
			pack.bvh.GetIntersection(ref query);

			//Must use exact comparison to check for modification
			//Compare before multiplication to avoid float math issues
			bool skip = oldDistance == query.distance;

			query.ray = oldRay;
			query.distance *= backwardScale;
			query.instance = oldInstance;

			//If the distance did not change, it means no intersection made thus we can skip
			if (skip) return;
			CheckToken(ref query);

			//We have to transform the normal from local to parent space
			query.normal = backwardTransform.MultiplyDirection(query.normal) * forwardScale;
		}

		public void GetIntersectionRoot(ref HitQuery query)
		{
			query.instance = this;
			pack.bvh.GetIntersection(ref query);

			CheckToken(ref query);
			query.instance = null;
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
		/// Transforms <paramref name="ray"/> from parent to local space and returns the new ray.
		/// </summary>
		Ray TransformForward(in Ray ray)
		{
			Float3 origin = forwardTransform.MultiplyPoint(ray.origin);
			Float3 direction = forwardTransform.MultiplyDirection(ray.direction);

			return new Ray(origin, direction * backwardScale);
		}

		/// <summary>
		/// If we just hit a geometry in this pack, this method assigns
		/// the appropriate information (normal) to <see cref="query"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void CheckToken(ref HitQuery query)
		{
			if (query.token.instance != id) return;

			pack.GetNormal(ref query);
		}
	}
}