using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Objects.Scenes;

namespace EchoRenderer.Objects.Preparation
{
	public class PreparedInstance
	{
		/// <summary>
		/// Creates a regular <see cref="PreparedInstance"/>.
		/// </summary>
		public PreparedInstance(ScenePreparer preparer, ObjectInstance instance) : this(preparer, instance.ObjectPack, instance.Mapper)
		{
			forwardTransform = instance.WorldToLocal;
			inverseTransform = instance.LocalToWorld;

			Float3 scale = instance.Scale;

			if (scale.Sorted == scale.SortedReversed)
			{
				inverseScale = scale.Average;
				forwardScale = 1f / inverseScale;
			}
			else throw new Exception($"{nameof(ObjectInstance)} does not support none uniform scaling! '{scale}'");
		}

		/// <summary>
		/// Creates a root <see cref="PreparedInstance"/> with <paramref name="scene"/>.
		/// </summary>
		public PreparedInstance(ScenePreparer preparer, Scene scene) : this(preparer, scene, null)
		{
			forwardTransform = Float4x4.identity;
			inverseTransform = Float4x4.identity;

			forwardScale = 1f;
			inverseScale = 1f;
		}

		PreparedInstance(ScenePreparer preparer, ObjectPack pack, MaterialMapper mapper)
		{
			id = preparer.RegisterPreparedInstance(this);
			this.pack = preparer.GetPreparedPack(pack);
			this.mapper = preparer.materials.GetMapper(mapper);
		}

		/// <summary>
		/// Computes the <see cref="AxisAlignedBoundingBox"/> of all contents inside this instance based on its parent's coordinate system.
		/// This <see cref="AxisAlignedBoundingBox"/> does not necessary enclose the root, only the enclosure of the content is guaranteed.
		/// NOTE: This property could be slow, so if performance issues arise try to memoize the result.
		/// </summary>
		public AxisAlignedBoundingBox AABB => pack.aggregator.GetTransformedAABB(inverseTransform);

		public readonly uint id;
		public readonly PreparedPack pack;
		public readonly MaterialPreparer.Mapper mapper;

		readonly Float4x4 forwardTransform; //The parent to local transform
		readonly Float4x4 inverseTransform; //The local to parent transform

		readonly float forwardScale; //The parent to local scale multiplier
		readonly float inverseScale; //The local to parent scale multiplier

		/// <summary>
		/// Processes <paramref name="query"/>.
		/// </summary>
		public void Trace(ref TraceQuery query)
		{
			var oldRay = query.ray;

			//Convert from parent space to local space
			TransformForward(ref query.ray);
			query.distance *= forwardScale;
			query.current.Push(this);

			//Gets intersection from aggregator in local space
			pack.aggregator.Trace(ref query);

			//Convert back to parent space
			query.ray = oldRay;
			query.distance *= inverseScale;
			query.current.Pop();
		}

		/// <summary>
		/// Processes <paramref name="query"/> as a <see cref="PreparedInstance"/> root.
		/// </summary>
		public void TraceRoot(ref TraceQuery query)
		{
			Assert.IsTrue(query.current.Equals(default));
			pack.aggregator.Trace(ref query);
		}

		/// <summary>
		/// Processes <paramref name="query"/>.
		/// </summary>
		public void Occlude(ref OccludeQuery query) => throw new NotImplementedException();

		/// <summary>
		/// Processes <paramref name="query"/> as a <see cref="PreparedInstance"/> root.
		/// </summary>
		public void OccludeRoot(ref OccludeQuery query) => throw new NotImplementedException();

		/// <summary>
		/// Returns the cost of tracing a <see cref="TraceQuery"/>.
		/// </summary>
		public int TraceCost(in Ray ray, ref float distance)
		{
			//Forward transform distance to local space
			distance *= forwardScale;

			//Gets intersection cost from bvh, calculation done in local space
			Ray transformed = ray;
			TransformForward(ref transformed);

			int cost = pack.aggregator.TraceCost(transformed, ref distance);

			//Transforms distance back to parent space
			distance *= inverseScale;
			return cost;
		}

		/// <summary>
		/// Transforms <paramref name="direction"/> from local space to parent space.
		/// NOTE: this might apply a uniformed scaling to <paramref name="direction"/>.
		/// </summary>
		public void TransformInverse(ref Float3 direction)
		{
			direction = inverseTransform.MultiplyDirection(direction);
		}

		/// <summary>
		/// Transforms <paramref name="ray"/> from parent to local space.
		/// </summary>
		void TransformForward(ref Ray ray)
		{
			Float3 origin = forwardTransform.MultiplyPoint(ray.origin);
			Float3 direction = forwardTransform.MultiplyDirection(ray.direction);

			ray = new Ray(origin, direction * inverseScale);
		}
	}
}