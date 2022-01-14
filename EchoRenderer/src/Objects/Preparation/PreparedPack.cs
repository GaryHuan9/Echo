using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Objects.GeometryObjects;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.Profiles;

namespace EchoRenderer.Objects.Preparation
{
	public class PreparedPack
	{
		public PreparedPack(ScenePreparer preparer, ObjectPack source)
		{
			var trianglesList = new ConcurrentList<PreparedTriangle>();
			var spheresList = new List<PreparedSphere>();
			var instancesList = new List<PreparedInstance>();

			trianglesList.BeginAdd();

			foreach (Object child in source.LoopChildren(true))
			{
				switch (child)
				{
					case GeometryObject geometry:
					{
						trianglesList.AddRange(geometry.ExtractTriangles(preparer.materials).Where(triangle => triangle.materialToken >= 0));
						spheresList.AddRange(geometry.ExtractSpheres(preparer.materials).Where(sphere => sphere.materialToken >= 0));

						break;
					}
					case ObjectInstance packInstance:
					{
						instancesList.Add(new PreparedInstance(preparer, packInstance));
						break;
					}
				}
			}

			trianglesList.EndAdd();

			SubdivideTriangles(trianglesList, preparer.profile);

			//Extract prepared data
			triangles = new PreparedTriangle[trianglesList.Count];
			spheres = new PreparedSphere[spheresList.Count];
			instances = new PreparedInstance[instancesList.Count];

			geometryCounts = new GeometryCounts(triangles.Length, spheres.Length, instances.Length);

			//Construct bounding volume hierarchy acceleration structure
			Token[] tokens = new Token[geometryCounts.Total];
			var aabbs = new AxisAlignedBoundingBox[tokens.Length];

			Parallel.For(0, triangles.Length, FillTriangles);
			Parallel.For(0, spheres.Length, FillSpheres);
			Parallel.For(0, instances.Length, FillInstances);

			void FillTriangles(int index)
			{
				var triangle = trianglesList[index];
				triangles[index] = triangle;

				aabbs[index] = triangle.AABB;
				tokens[index] = Token.CreateTriangle((uint)index);
			}

			void FillSpheres(int index)
			{
				var sphere = spheresList[index];
				spheres[index] = sphere;

				int target = triangles.Length + index;

				aabbs[target] = sphere.AABB;
				tokens[target] = Token.CreateSphere((uint)index);
			}

			void FillInstances(int index)
			{
				var instance = instancesList[index];
				instances[index] = instance;

				int target = triangles.Length + spheres.Length + index;

				aabbs[target] = instance.AABB;
				tokens[target] = Token.CreateInstance((uint)index);
			}

			aggregator = preparer.profile.AggregatorProfile.CreateAggregator(this, aabbs, tokens);
		}

		public readonly Aggregator aggregator;
		public readonly GeometryCounts geometryCounts;

		readonly PreparedTriangle[] triangles;
		readonly PreparedSphere[] spheres;
		readonly PreparedInstance[] instances;

		/// <summary>
		/// If an intersection has a distance under this value and we just intersected the exactly same geometry with the last query,
		/// we will ignore this intersection. NOTE: because spheres have two intersection points, <see cref="PreparedSphere"/>'s get
		/// intersection method must return the point with a distance larger than or equals to this value.
		/// </summary>
		public const float DistanceMin = 6e-4f;

		/// <summary>
		/// Calculates the intersection between <paramref name="query"/> and object represented with <paramref name="token"/>.
		/// If the intersection occurs before the original <paramref name="query.distance"/>, then the intersection is recorded.
		/// </summary>
		public void Trace(ref TraceQuery query, in Token token)
		{
			Assert.IsTrue(token.IsGeometry);

			if (token.IsTriangle)
			{
				query.current.geometry = token;
				if (query.current == query.ignore) return;

				ref readonly var triangle = ref triangles[token.TriangleValue];
				float distance = triangle.Intersect(query.ray, out Float2 uv);

				query.token = query.current;
				query.distance = distance;
				query.uv = uv;
			}
			else if (token.IsSphere)
			{
				ref readonly var sphere = ref spheres[token.SphereValue];
				float distance = sphere.Intersect(query.ray, out Float2 uv);

				if (distance >= query.distance) return;

				query.current.geometry = token;
				if (distance < DistanceMin && query.ignore == query.current) return;

				query.token = query.current;
				query.distance = distance;
				query.uv = uv;
			}
			else instances[token.InstanceValue].Trace(ref query);
		}

		/// <summary>
		/// Calculates and returns whether <paramref name="query"/> is
		/// occluded by the object represented with <paramref name="token"/>.
		/// </summary>
		public bool Occlude(ref OccludeQuery query, in Token token)
		{
			Assert.IsTrue(token.IsGeometry);

			throw new NotImplementedException();

			// if (token.IsTriangle)
			// {
			// 	query.current.geometry = token;
			// 	if (query.current == query.ignore) return;
			//
			// 	ref readonly var triangle = ref triangles[token.TriangleValue];
			// 	float distance = triangle.Intersect(query.ray, out Float2 uv);
			//
			// 	query.token = query.current;
			// 	query.distance = distance;
			// 	query.uv = uv;
			// }
			// else if (token.IsSphere)
			// {
			// 	ref readonly var sphere = ref spheres[token.SphereValue];
			// 	float distance = sphere.Intersect(query.ray, out Float2 uv);
			//
			// 	if (distance >= query.distance) return;
			//
			// 	query.current.geometry = token;
			// 	if (distance < DistanceMin && query.ignore == query.current) return;
			//
			// 	query.token = query.current;
			// 	query.distance = distance;
			// 	query.uv = uv;
			// }

			return instances[token.InstanceValue].Occlude(ref query);
		}

		/// <summary>
		/// Returns the cost of an intersection calculation of <paramref name="ray"/> with this current <see cref="PreparedPack"/>.
		/// </summary>
		public int GetTraceCost(in Ray ray, ref float distance, in Token token)
		{
			Assert.IsTrue(token.IsGeometry);

			if (token.IsTriangle)
			{
				ref readonly var triangle = ref triangles[token.TriangleValue];
				float hit = triangle.Intersect(ray, out Float2 _);

				distance = Math.Min(distance, hit);
				return 1;
			}

			if (token.IsSphere)
			{
				ref readonly var sphere = ref spheres[token.SphereValue];
				float hit = sphere.Intersect(ray, out Float2 _);

				distance = Math.Min(distance, hit);
				return 1;
			}

			return instances[token.InstanceValue].TraceCost(ray, ref distance);
		}

		/// <summary>
		/// Begins interacting with the result of <paramref name="query"/> by returning
		/// the <see cref="Interaction"/> and outputting the <paramref name="material"/>.
		/// </summary>
		public Interaction Interact(in TraceQuery query, ScenePreparer preparer, PreparedInstance instance, out Material material)
		{
			Token token = query.token.geometry;

			Assert.IsTrue(token.IsGeometry);
			Assert.AreEqual(instance.pack, this);

			int materialToken;
			Float3 geometryNormal;
			Float3 normal;
			Float2 texcoord;

			if (token.IsTriangle)
			{
				ref readonly var triangle = ref triangles[token.TriangleValue];

				materialToken = triangle.materialToken;
				geometryNormal = triangle.GeometryNormal;
				normal = triangle.GetNormal(query.uv);
				texcoord = triangle.GetTexcoord(query.uv);

				ApplyWorldTransform(query.token, preparer, ref geometryNormal);
				ApplyWorldTransform(query.token, preparer, ref normal);
			}
			else if (token.IsSphere)
			{
				ref readonly var sphere = ref spheres[token.SphereValue];

				materialToken = sphere.materialToken;
				texcoord = query.uv; //Sphere directly uses the uv as texcoord

				normal = PreparedSphere.GetGeometryNormal(query.uv);
				ApplyWorldTransform(query.token, preparer, ref normal);
				geometryNormal = normal;
			}
			else
			{
				//Handles tokens of PreparedInstance, which is invalid since tokens should be resolved to pure geometry after intersection calculation
				throw new Exception($"{nameof(Interact)} should be invoked on the base {nameof(PreparedPack)}, not one with a token that is a pack instance!");
			}

			material = instance.mapper[materialToken];
			material.ApplyNormalMapping(texcoord, ref normal);
			return new Interaction(query, geometryNormal, normal, texcoord);

			static void ApplyWorldTransform(in GeometryToken token, ScenePreparer preparer, ref Float3 direction)
			{
				ReadOnlySpan<uint> ids = token.Instances;

				for (int i = ids.Length - 1; i >= 0; i--)
				{
					var instance = preparer.GetPreparedInstance(ids[i]);
					instance.TransformInverse(ref direction);
				}

				direction = direction.Normalized;
			}
		}

		/// <summary>
		/// Divides large triangles for better space partitioning.
		/// </summary>
		static void SubdivideTriangles(ConcurrentList<PreparedTriangle> triangles, ScenePrepareProfile profile)
		{
			double totalArea = 0d;

			Parallel.ForEach(triangles, triangle => InterlockedHelper.Add(ref totalArea, triangle.Area));
			float threshold = (float)(totalArea / triangles.Count * profile.FragmentationThresholdMultiplier);

			using var _ = triangles.BeginAdd();

			Parallel.For(0, triangles.Count, index => SubdivideTriangle(triangles, ref triangles[index], threshold, profile.FragmentationMaxIteration));
		}

		static void SubdivideTriangle(ConcurrentList<PreparedTriangle> triangles, ref PreparedTriangle triangle, float threshold, int maxIteration)
		{
			float multiplier = MathF.Log2(triangle.Area / threshold);
			int iteration = Math.Min(multiplier.Ceil(), maxIteration);
			if (iteration <= 0) return;

			int subdivision = 1 << (iteration * 2);
			Span<PreparedTriangle> divided = stackalloc PreparedTriangle[subdivision];

			triangle.GetSubdivided(divided, iteration);

			triangle = divided[0];
			for (int i = 1; i < subdivision; i++) triangles.Add(divided[i]);
		}
	}
}