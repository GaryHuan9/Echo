using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using EchoRenderer.Objects;
using EchoRenderer.Objects.GeometryObjects;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Textures;
using Object = EchoRenderer.Objects.Object;

namespace EchoRenderer.Mathematics.Intersections
{
	public class PressedPack
	{
		public PressedPack(ObjectPack source, ScenePresser presser)
		{
			List<PressedTriangle> trianglesList = new List<PressedTriangle>();
			List<PressedSphere> spheresList = new List<PressedSphere>();
			List<PressedPackInstance> instancesList = new List<PressedPackInstance>();

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
						instancesList.Add(new PressedPackInstance(packInstance, presser));
						break;
					}
				}
			}

			SubdivideTriangles(trianglesList);

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

			Thread.MemoryBarrier();

			//Un-references large intermediate lists for GC
			trianglesList = null;
			spheresList = null;
			instancesList = null;

			bvh = new BoundingVolumeHierarchy(this, aabbs, tokens);

			void FillTriangles(int index)
			{
				var triangle = trianglesList[index];
				triangles[index] = triangle;

				aabbs[index] = triangle.AABB;
				tokens[index] = (uint)(index + TrianglesTreshold);
			}

			void FillSpheres(int index)
			{
				var sphere = spheresList[index];
				spheres[index] = sphere;

				int target = triangles.Length + index;

				aabbs[target] = sphere.AABB;
				tokens[target] = (uint)(index + SpheresTreshold);
			}

			void FillInstances(int index)
			{
				var instance = instancesList[index];
				instances[index] = instance;

				int target = triangles.Length + spheres.Length + index;

				aabbs[target] = instance.AABB;
				tokens[target] = (uint)index;
			}
		}

		public readonly BoundingVolumeHierarchy bvh;
		public readonly GeometryCounts geometryCounts;

		readonly PressedTriangle[] triangles;     //Indices: [0x8000_0000 to 0xFFFF_FFFF)
		readonly PressedSphere[] spheres;         //Indices: [0x4000_0000 to 0x8000_0000)
		readonly PressedPackInstance[] instances; //Indices: [0 to 0x4000_0000)

		const uint TrianglesTreshold = 0x8000_0000u;
		const uint SpheresTreshold = 0x4000_0000u;

		/// <summary>
		/// Calculates the intersection between <paramref name="ray"/> and object with <paramref name="token"/>.
		/// If the intersection occurs before the original <paramref name="hit.distance"/>, then the intersection is recorded.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void GetIntersection(in Ray ray, ref Hit hit, uint token)
		{
			switch (token)
			{
				case >= TrianglesTreshold:
				{
					ref PressedTriangle triangle = ref triangles[token - TrianglesTreshold];
					float distance = triangle.GetIntersection(ray, out Float2 uv);

					if (distance < hit.distance)
					{
						hit.instance = null;
						hit.distance = distance;

						hit.token = token;
						hit.uv = uv;
					}

					break;
				}
				case >= SpheresTreshold:
				{
					ref PressedSphere sphere = ref spheres[token - SpheresTreshold];
					float distance = sphere.GetIntersection(ray, out Float2 uv);

					if (distance < hit.distance)
					{
						hit.instance = null;
						hit.distance = distance;

						hit.token = token;
						hit.uv = uv;
					}

					break;
				}
				default:
				{
					instances[token].GetIntersection(ray, ref hit);
					break;
				}
			}
		}

		/// <summary>
		/// Calculates the intersection between <paramref name="ray"/> and object with <paramref name="token"/>.
		/// If the intersection occurs before the original <paramref name="distance"/>, then the intersection is replaced.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void GetIntersection(in Ray ray, ref float distance, uint token)
		{
			switch (token)
			{
				case >= TrianglesTreshold:
				{
					ref PressedTriangle triangle = ref triangles[token - TrianglesTreshold];
					distance = Math.Min(distance, triangle.GetIntersection(ray));

					break;
				}
				case >= SpheresTreshold:
				{
					ref PressedSphere sphere = ref spheres[token - SpheresTreshold];
					distance = Math.Min(distance, sphere.GetIntersection(ray));

					break;
				}
				default:
				{
					instances[token].GetIntersection(ray, ref distance);
					break;
				}
			}
		}

		/// <summary>
		/// Returns the cost of an intersection calculation of <paramref name="ray"/> with this current <see cref="PressedPack"/>.
		/// </summary>
		public int GetIntersectionCost(in Ray ray, ref float distance, uint token)
		{
			switch (token)
			{
				case >= TrianglesTreshold:
				{
					ref PressedTriangle triangle = ref triangles[token - TrianglesTreshold];
					float hit = triangle.GetIntersection(ray, out Float2 _);

					distance = Math.Min(distance, hit);
					return 1;
				}
				case >= SpheresTreshold:
				{
					ref PressedSphere sphere = ref spheres[token - SpheresTreshold];
					float hit = sphere.GetIntersection(ray, out Float2 _);

					distance = Math.Min(distance, hit);
					return 1;
				}
				default: return instances[token].GetIntersectionCost(ray, ref distance);
			}
		}

		/// <summary>
		/// Calculates the normal of <paramref name="hit"/> and assigns it back to <paramref name="hit.normal"/>.
		/// </summary>
		public void GetNormal(ref Hit hit)
		{
			switch (hit.token)
			{
				case >= TrianglesTreshold:
				{
					ref PressedTriangle triangle = ref triangles[hit.token - TrianglesTreshold];
					hit.normal = triangle.GetNormal(hit.uv);

					break;
				}
				case >= SpheresTreshold:
				{
					ref PressedSphere sphere = ref spheres[hit.token - SpheresTreshold];
					hit.normal = sphere.GetNormal(hit.uv);

					break;
				}
				default: throw new Exception($"{nameof(GetNormal)} cannot be used to get the normal of a {nameof(PressedPackInstance)}!");
			}
		}

		/// <summary>
		/// Creates a <see cref="CalculatedHit"/> from <paramref name="hit"/> and <paramref name="ray"/> of this pack.
		/// </summary>
		public CalculatedHit CreateHit(in Hit hit, in Ray ray, MaterialPresser.Mapper mapper)
		{
			int material;
			Float2 texcoord;

			switch (hit.token)
			{
				case >= TrianglesTreshold:
				{
					ref PressedTriangle triangle = ref triangles[hit.token - TrianglesTreshold];

					material = triangle.materialToken;
					texcoord = triangle.GetTexcoord(hit.uv);

					break;
				}
				case >= SpheresTreshold:
				{
					ref PressedSphere sphere = ref spheres[hit.token - SpheresTreshold];

					material = sphere.materialToken;
					texcoord = hit.uv; //Sphere directly uses the uv as texcoord

					break;
				}
				default: throw new Exception($"{nameof(CreateHit)} should be invoked on the base {nameof(PressedPack)}, which is {hit.instance}!");
			}

			Float3 point = ray.GetPoint(hit.distance);

			return new CalculatedHit
			(
				point, ray.direction, hit.distance,
				mapper[material], hit.normal, texcoord
			);
		}

		/// <summary>
		/// Divides large triangles for better BVH space partitioning.
		/// </summary>
		static void SubdivideTriangles(List<PressedTriangle> triangles)
		{
			double totalArea = 0d;

			Parallel.ForEach(triangles, triangle => InterlockedHelper.Add(ref totalArea, triangle.Area));

			const float ThresholdMultiplier = 4.8f; //How many times does an area has to be over the average to trigger a fragmentation
			const int MaxIteration = 3;             //The maximum number of fragmentation that can happen to one source triangle

			float threshold = (float)(totalArea / triangles.Count * ThresholdMultiplier);
			using ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

			Parallel.For(0, triangles.Count, SubdivideSingle);

			void SubdivideSingle(int index)
			{
				PressedTriangle triangle;

				locker.EnterReadLock();
				try { triangle = triangles[index]; }
				finally { locker.ExitReadLock(); }

				float multiplier = triangle.Area / threshold;

				int iteration = Math.Min(MathF.Log2(multiplier).Ceil(), MaxIteration);
				if (iteration <= 0) return;

				int subdivision = 1 << (iteration * 2);
				Span<PressedTriangle> divided = stackalloc PressedTriangle[subdivision];

				triangle.GetSubdivided(divided, iteration);

				locker.EnterWriteLock();
				try
				{
					triangles[index] = divided[0];
					for (int i = 1; i < subdivision; i++) triangles.Add(divided[i]);
				}
				finally { locker.ExitWriteLock(); }
			}
		}
	}
}