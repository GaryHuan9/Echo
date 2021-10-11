using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Objects;
using EchoRenderer.Objects.GeometryObjects;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.Profiles;
using Object = EchoRenderer.Objects.Object;

namespace EchoRenderer.Mathematics.Accelerators
{
	public class PressedPack
	{
		public PressedPack(ObjectPack source, ScenePresser presser)
		{
			var trianglesList = new ConcurrentList<PressedTriangle>();
			var spheresList = new List<PressedSphere>();
			var instancesList = new List<PressedPackInstance>();

			trianglesList.BeginAdd();

			foreach (Object child in source.LoopChildren(true))
			{
				switch (child)
				{
					case GeometryObject geometry:
					{
						trianglesList.AddRange(geometry.ExtractTriangles(presser.materials).Where(triangle => triangle.materialToken >= 0));
						spheresList.AddRange(geometry.ExtractSpheres(presser.materials).Where(sphere => sphere.materialToken >= 0));

						break;
					}
					case ObjectPackInstance packInstance:
					{
						instancesList.Add(new PressedPackInstance(presser, packInstance));
						break;
					}
				}
			}

			trianglesList.EndAdd();

			SubdivideTriangles(trianglesList, presser.profile);

			//Extract pressed data
			triangles = new PressedTriangle[trianglesList.Count];
			spheres = new PressedSphere[spheresList.Count];
			instances = new PressedPackInstance[instancesList.Count];

			geometryCounts = new GeometryCounts(triangles.Length, spheres.Length, instances.Length);

			//Construct bounding volume hierarchy acceleration structure
			uint[] tokens = new uint[geometryCounts.Total];
			var aabbs = new AxisAlignedBoundingBox[tokens.Length];

			Parallel.For(0, triangles.Length, FillTriangles);
			Parallel.For(0, spheres.Length, FillSpheres);
			Parallel.For(0, instances.Length, FillInstances);

			void FillTriangles(int index)
			{
				var triangle = trianglesList[index];
				triangles[index] = triangle;

				aabbs[index] = triangle.AABB;
				tokens[index] = (uint)(index + TrianglesThreshold);
			}

			void FillSpheres(int index)
			{
				var sphere = spheresList[index];
				spheres[index] = sphere;

				int target = triangles.Length + index;

				aabbs[target] = sphere.AABB;
				tokens[target] = (uint)(index + SpheresThreshold);
			}

			void FillInstances(int index)
			{
				var instance = instancesList[index];
				instances[index] = instance;

				int target = triangles.Length + spheres.Length + index;

				aabbs[target] = instance.AABB;
				tokens[target] = (uint)index;
			}

			//Un-references large intermediate lists for GC
			trianglesList = null;
			spheresList = null;
			instancesList = null;

			accelerator = presser.profile.AcceleratorProfile.CreateAccelerator(this, aabbs, tokens);
		}

		public readonly TraceAccelerator accelerator;
		public readonly GeometryCounts geometryCounts;

		readonly PressedTriangle[] triangles;     //Indices: [0x4000_0000 to 0x8000_0000)
		readonly PressedSphere[] spheres;         //Indices: [0x2000_0000 to 0x4000_0000)
		readonly PressedPackInstance[] instances; //Indices: [0 to 0x2000_0000)

		/// <summary>
		/// If an intersection has a distance under this value and we just intersected the exactly same geometry with the last query,
		/// we will ignore this intersection. NOTE: because spheres have two intersection points, <see cref="PressedSphere"/>'s get
		/// intersection method must return the point with a distance larger than or equals to this value.
		/// </summary>
		public const float DistanceMin = 6e-4f;

		const uint NodeThreshold = TraceAccelerator.NodeThreshold;
		const uint TrianglesThreshold = 0x4000_0000u;
		const uint SpheresThreshold = 0x2000_0000u;

		/// <summary>
		/// Calculates the intersection between <paramref name="query"/> and object with <paramref name="token"/>.
		/// If the intersection occurs before the original <paramref name="query.distance"/>, then the intersection is recorded.
		/// </summary>
		public void GetIntersection(ref HitQuery query, uint token)
		{
			Assert.IsTrue(token < NodeThreshold);

			switch (token)
			{
				case >= TrianglesThreshold:
				{
					ref PressedTriangle triangle = ref triangles[token - TrianglesThreshold];
					float distance = triangle.GetIntersection(query.ray, out Float2 uv);

					if (!ValidateDistance(distance, ref query, token)) return;

					query.uv = uv;
					break;
				}
				case >= SpheresThreshold:
				{
					ref PressedSphere sphere = ref spheres[token - SpheresThreshold];
					float distance = sphere.GetIntersection(query.ray, out Float2 uv);

					if (!ValidateDistance(distance, ref query, token)) return;

					query.uv = uv;
					break;
				}
				default:
				{
					instances[token].GetIntersection(ref query);
					break;
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static bool ValidateDistance(float distance, ref HitQuery hit, uint token)
			{
				if (distance >= hit.distance) return false;

				GeometryToken geometryToken = new GeometryToken(hit.instance, token);
				if (distance < DistanceMin && hit.previous == geometryToken) return false;

				hit.token = geometryToken;
				hit.distance = distance;

				return true;
			}
		}

		/// <summary>
		/// Returns the cost of an intersection calculation of <paramref name="ray"/> with this current <see cref="PressedPack"/>.
		/// </summary>
		public int GetIntersectionCost(in Ray ray, ref float distance, uint token)
		{
			Assert.IsTrue(token < NodeThreshold);

			switch (token)
			{
				case >= TrianglesThreshold:
				{
					ref PressedTriangle triangle = ref triangles[token - TrianglesThreshold];
					float hit = triangle.GetIntersection(ray, out Float2 _);

					distance = Math.Min(distance, hit);
					return 1;
				}
				case >= SpheresThreshold:
				{
					ref PressedSphere sphere = ref spheres[token - SpheresThreshold];
					float hit = sphere.GetIntersection(ray, out Float2 _);

					distance = Math.Min(distance, hit);
					return 1;
				}
				default: return instances[token].GetIntersectionCost(ray, ref distance);
			}
		}

		/// <summary>
		/// Calculates the normal of <paramref name="query"/> and assigns it back to <paramref name="query.normal"/>.
		/// </summary>
		public void GetNormal(ref HitQuery query)
		{
			uint token = query.token.geometry;
			Assert.IsTrue(token < NodeThreshold);

			switch (token)
			{
				case >= TrianglesThreshold:
				{
					ref PressedTriangle triangle = ref triangles[token - TrianglesThreshold];
					query.normal = triangle.GetNormal(query.uv);

					break;
				}
				case >= SpheresThreshold:
				{
					// ref PressedSphere sphere = ref spheres[token - SpheresThreshold];
					query.normal = PressedSphere.GetNormal(query.uv);

					break;
				}
				default: throw new Exception($"{nameof(GetNormal)} cannot be used to get the normal of a {nameof(PressedPackInstance)}!");
			}
		}

		/// <summary>
		/// Fills the appropriate <see cref="HitQuery.Shading"/> information for <see cref="query"/>.
		/// </summary>
		public void FillShading(ref HitQuery query, PressedPackInstance instance)
		{
			ref Float2 texcoord = ref query.shading.texcoord;

			int materialToken;
			uint token = query.token.geometry;

			Assert.AreEqual(instance.pack, this);
			Assert.IsTrue(token < NodeThreshold);

			switch (token)
			{
				case >= TrianglesThreshold:
				{
					ref PressedTriangle triangle = ref triangles[token - TrianglesThreshold];

					materialToken = triangle.materialToken;
					texcoord = triangle.GetTexcoord(query.uv);

					break;
				}
				case >= SpheresThreshold:
				{
					ref PressedSphere sphere = ref spheres[token - SpheresThreshold];

					materialToken = sphere.materialToken;
					texcoord = query.uv; //Sphere directly uses the uv as texcoord

					break;
				}

				//The default case handles tokens of PressedPackInstance, which is invalid since tokens should be resolved to pure geometry after intersection calculation
				default: throw new Exception($"{nameof(FillShading)} should be invoked on the base {nameof(PressedPack)}, not one with a token that is a pack instance!");
			}

			ref Material material = ref query.shading.material;

			material = instance.mapper[materialToken];
			material.FillTangentNormal(ref query);
		}

		/// <summary>
		/// Divides large triangles for better space partitioning.
		/// </summary>
		static void SubdivideTriangles(ConcurrentList<PressedTriangle> triangles, ScenePressProfile profile)
		{
			double totalArea = 0d;

			Parallel.ForEach(triangles, triangle => InterlockedHelper.Add(ref totalArea, triangle.Area));
			float threshold = (float)(totalArea / triangles.Count * profile.ThresholdMultiplier);

			using var _ = triangles.BeginAdd();

			Parallel.For(0, triangles.Count, index => SubdivideTriangle(triangles, ref triangles[index], threshold, profile.MaxIteration));
		}

		static void SubdivideTriangle(ConcurrentList<PressedTriangle> triangles, ref PressedTriangle triangle, float threshold, int maxIteration)
		{
			float multiplier = MathF.Log2(triangle.Area / threshold);
			int iteration = Math.Min(multiplier.Ceil(), maxIteration);
			if (iteration <= 0) return;

			int subdivision = 1 << (iteration * 2);
			Span<PressedTriangle> divided = stackalloc PressedTriangle[subdivision];

			triangle.GetSubdivided(divided, iteration);

			triangle = divided[0];
			for (int i = 1; i < subdivision; i++) triangles.Add(divided[i]);
		}
	}
}