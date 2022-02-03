using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Rendering.Distributions;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.Profiles;
using EchoRenderer.Scenic.Geometries;
using EchoRenderer.Scenic.Instancing;

namespace EchoRenderer.Scenic.Preparation
{
	public class PreparedPack
	{
		public PreparedPack(ScenePreparer preparer, EntityPack pack)
		{
			var trianglesList = new ConcurrentList<PreparedTriangle>();
			var spheresList = new List<PreparedSphere>();
			var instancesList = new List<PreparedInstance>();

			swatchExtractor = new SwatchExtractor(preparer);

			trianglesList.BeginAdd();

			foreach (Entity child in pack.LoopChildren(true))
			{
				switch (child)
				{
					case GeometryEntity geometry:
					{
						trianglesList.AddRange(geometry.ExtractTriangles(swatchExtractor));
						spheresList.AddRange(geometry.ExtractSpheres(swatchExtractor));

						break;
					}
					case PackInstance objectInstance:
					{
						uint id = (uint)instancesList.Count;
						var instance = new PreparedInstance(preparer, objectInstance, id);
						instancesList.Add(instance);

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

			//Construct aggregator
			Token[] tokens = new Token[geometryCounts.Total];
			var aabbs = new AxisAlignedBoundingBox[tokens.Length];

			Parallel.For(0, triangles.Length, FillTriangles);
			Parallel.For(0, spheres.Length, FillSpheres);
			Parallel.For(0, instances.Length, FillInstances);

			aggregator = preparer.profile.AggregatorProfile.CreateAggregator(this, aabbs, tokens);

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
		}

		public readonly Aggregator aggregator;
		public readonly GeometryCounts geometryCounts;
		public readonly SwatchExtractor swatchExtractor;

		readonly PreparedTriangle[] triangles;
		readonly PreparedSphere[] spheres;
		readonly PreparedInstance[] instances;

		/// <summary>
		/// Calculates the intersection between <paramref name="query"/> and the object represented by <paramref name="token"/>.
		/// The intersection is only recorded if it occurs before the original <paramref name="query.distance"/>.
		/// </summary>
		public void Trace(ref TraceQuery query, in Token token)
		{
			Assert.IsTrue(token.IsGeometry);
			query.current.Geometry = token;

			if (token.IsTriangle)
			{
				if (query.ignore == query.current) return;

				ref readonly var triangle = ref triangles[token.TriangleValue];
				float distance = triangle.Intersect(query.ray, out Float2 uv);

				if (distance >= query.distance) return;

				query.token = query.current;
				query.distance = distance;
				query.uv = uv;
			}
			else if (token.IsSphere)
			{
				bool findFar = query.ignore == query.current;

				ref readonly PreparedSphere sphere = ref spheres[token.SphereValue];
				float distance = sphere.Intersect(query.ray, out Float2 uv, findFar);

				if (distance >= query.distance) return;

				query.token = query.current;
				query.distance = distance;
				query.uv = uv;
			}
			else instances[token.InstanceValue].Trace(ref query);
		}

		/// <summary>
		/// Calculates and returns whether <paramref name="query"/> is occluded by the object represented by <paramref name="token"/>.
		/// </summary>
		public bool Occlude(ref OccludeQuery query, in Token token)
		{
			Assert.IsTrue(token.IsGeometry);
			query.current.Geometry = token;

			if (token.IsTriangle)
			{
				if (query.ignore == query.current) return false;

				ref readonly var triangle = ref triangles[token.TriangleValue];
				return triangle.Intersect(query.ray, query.travel);
			}

			if (token.IsSphere)
			{
				bool findFar = query.ignore == query.current;

				ref readonly var sphere = ref spheres[token.SphereValue];
				return sphere.Intersect(query.ray, query.travel, findFar);
			}

			return instances[token.InstanceValue].Occlude(ref query);
		}

		/// <summary>
		/// Returns the cost of an intersection calculation between <paramref name="ray"/> and the object represented by <paramref name="token"/>.
		/// </summary>
		public int GetTraceCost(in Ray ray, ref float distance, in Token token)
		{
			Assert.IsTrue(token.IsGeometry);

			if (token.IsTriangle)
			{
				ref readonly var triangle = ref triangles[token.TriangleValue];
				distance = Math.Min(distance, triangle.Intersect(ray, out _));
				return 1;
			}

			if (token.IsSphere)
			{
				ref readonly var sphere = ref spheres[token.SphereValue];
				distance = Math.Min(distance, sphere.Intersect(ray, out _));
				return 1;
			}

			return instances[token.InstanceValue].TraceCost(ray, ref distance);
		}

		/// <summary>
		/// Returns the <see cref="PreparedInstance"/> stored in this <see cref="PreparedPack"/> with <paramref name="id"/>.
		/// </summary>
		public PreparedInstance GetInstance(uint id) => instances[id];

		/// <inheritdoc cref="PreparedScene.Interact"/>
		public Interaction Interact(in TraceQuery query, in Float4x4 transform, PreparedInstance instance)
		{
			Token token = query.token.Geometry;

			Assert.IsTrue(token.IsGeometry);
			Assert.AreEqual(instance.pack, this);

			Float3 normal;
			Float2 texcoord;
			uint materialToken;

			if (token.IsTriangle)
			{
				ref readonly var triangle = ref triangles[token.TriangleValue];

				normal = triangle.GetNormal(query.uv);
				texcoord = triangle.GetTexcoord(query.uv);
				materialToken = triangle.materialToken;
			}
			else if (token.IsSphere)
			{
				ref readonly var sphere = ref spheres[token.SphereValue];

				normal = PreparedSphere.GetNormal(query.uv);
				texcoord = PreparedSphere.GetTexcoord(query.uv);
				materialToken = sphere.materialToken;
			}
			else throw NotBasePackException();

			normal = transform.MultiplyDirection(normal).Normalized; //Apply world transform to normal
			Material material = instance.swatch[materialToken];      //Find appropriate mapped material

			//Construct interaction
			if (material == null) return new Interaction(query, normal);
			return new Interaction(query, normal, material, texcoord);
		}

		/// <summary>
		/// <inheritdoc cref="PreparedScene.Sample"/>
		/// NOTE: this method functions according to the local coordinate system of this <see cref="PreparedPack"/>.
		/// </summary>
		public GeometryPoint Sample(in Token token, in Float3 origin, Distro2 distro, out float pdf)
		{
			Assert.IsTrue(token.IsGeometry);

			if (token.IsTriangle)
			{
				ref readonly var triangle = ref triangles[token.TriangleValue];
				return triangle.Sample(origin, distro, out pdf);
			}

			if (token.IsSphere)
			{
				ref readonly var sphere = ref spheres[token.SphereValue];
				return sphere.Sample(origin, distro, out pdf);
			}

			throw NotBasePackException();
		}

		/// <summary>
		/// <inheritdoc cref="PreparedScene.ProbabilityDensity"/>
		/// NOTE: this method functions according to the local coordinate system of this <see cref="PreparedPack"/>.
		/// </summary>
		public float ProbabilityDensity(in Token token, in Float3 origin, in Float3 incident)
		{
			Assert.IsTrue(token.IsGeometry);

			if (token.IsTriangle)
			{
				ref readonly var triangle = ref triangles[token.TriangleValue];
				return triangle.ProbabilityDensity(origin, incident);
			}

			if (token.IsSphere)
			{
				ref readonly var sphere = ref spheres[token.SphereValue];
				return sphere.ProbabilityDensity(origin, incident);
			}

			throw NotBasePackException();
		}

		/// <summary>
		/// Divides large triangles for better space partitioning.
		/// </summary>
		static void SubdivideTriangles(ConcurrentList<PreparedTriangle> triangles, ScenePrepareProfile profile)
		{
			if (profile.FragmentationMaxIteration == 0) return;

			double totalArea = 0d;

			Parallel.ForEach(triangles, triangle => InterlockedHelper.Add(ref totalArea, triangle.Area));
			float threshold = (float)(totalArea / triangles.Count * profile.FragmentationThreshold);

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

		/// <summary>
		/// Handles tokens of <see cref="PreparedInstance"/>, which is invalid since tokens should be resolved to pure geometry.
		/// </summary>
		static Exception NotBasePackException([CallerMemberName] string name = default) => new($"{name} should be invoked on the base {nameof(PreparedPack)}, not one with a token that is a {nameof(Token.IsInstance)}!");
	}
}