using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Files;
using CodeHelpers.Mathematics;
using CodeHelpers.ObjectPooling;
using ForceRenderer.Mathematics;
using ForceRenderer.Objects;
using ForceRenderer.Objects.SceneObjects;
using ForceRenderer.Rendering.Materials;
using ForceRenderer.Textures;
using Object = ForceRenderer.Objects.Object;

namespace ForceRenderer.Rendering
{
	/// <summary>
	/// A flattened out/pressed down record version of a scene for fast iteration.
	/// </summary>
	public class PressedScene
	{
		public PressedScene(Scene source)
		{
			ExceptionHelper.AssertMainThread();
			cubemap = source.Cubemap;

			List<PressedTriangle> triangleList = new List<PressedTriangle>();
			List<PressedSphere> sphereList = new List<PressedSphere>();
			List<PressedLight> lightList = new List<PressedLight>();

			Dictionary<Material, int> materialObjects = CollectionPooler<Material, int>.dictionary.GetObject();
			Queue<Object> frontier = CollectionPooler<Object>.queue.GetObject();

			//Find all rendering-related objects
			frontier.Enqueue(source);
			double totalArea = 0d; //Total area of all triangles combined

			while (frontier.Count > 0)
			{
				Object target = frontier.Dequeue();

				switch (target)
				{
					case SceneObject sceneObject:
					{
						int triangleCount = triangleList.Count;

						triangleList.AddRange(sceneObject.ExtractTriangles(ConvertMaterial).Where(triangle => triangle.materialToken >= 0));
						sphereList.AddRange(sceneObject.ExtractSpheres(ConvertMaterial).Where(sphere => sphere.materialToken >= 0));

						for (int i = triangleCount; i < triangleList.Count; i++) totalArea += triangleList[i].Area;

						break;

						int ConvertMaterial(Material material) //Converts a material into its material token
						{
							if (material is Invisible) return -1; //Negative token used to omit invisible materials

							if (!materialObjects.TryGetValue(material, out int materialToken))
							{
								materialToken = materialObjects.Count;
								materialObjects.Add(material, materialToken);
							}

							return materialToken;
						}
					}
					case Camera value:
					{
						if (camera == null) camera = value;
						else DebugHelper.Log($"Multiple {nameof(Camera)} found! Only the first one will be used.");

						break;
					}
					case Light value:
					{
						lightList.Add(new PressedLight(value));
						break;
					}
				}

				Object.Children children = target.children;
				for (int i = 0; i < children.Count; i++) frontier.Enqueue(children[i]);
			}

			//Divide large triangles for better BVH space partitioning
			const float DivideThresholdMultiplier = 4.8f; //How many times does an area has to be over the average to trigger a fragmentation
			const int DivideMaxIteration = 3;             //The maximum number of fragmentation that can happen to one source triangle

			float fragmentThreshold = (float)(totalArea / triangleList.Count * DivideThresholdMultiplier);
			int triangleListCount = triangleList.Count; //Cache list count so fragmented triangles are not fragmented again

			for (int i = 0; i < triangleListCount; i++)
			{
				float multiplier = triangleList[i].Area / fragmentThreshold;
				if (multiplier > 0f) Fragment(MathF.Log2(multiplier).Ceil());

				void Fragment(int iteration) //Should be placed in a local method because of stack allocation
				{
					iteration = iteration.Clamp(0, DivideMaxIteration);

					int subdivision = 1 << (iteration * 2);
					Span<PressedTriangle> divided = stackalloc PressedTriangle[subdivision];

					triangleList[i].GetSubdivided(divided, iteration);
					triangleList[i] = divided[0];

					for (int j = 1; j < subdivision; j++) triangleList.Add(divided[j]);
				}
			}

			//Extract pressed data
			materials = materialObjects.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToArray();
			for (int i = 0; i < materials.Length; i++) materials[i].Press();

			triangles = new PressedTriangle[triangleList.Count];
			spheres = new PressedSphere[sphereList.Count];

			lightList.TrimExcess();
			lights = new ReadOnlyCollection<PressedLight>(lightList);

			//Construct bounding volume hierarchy acceleration structure
			int[] tokens = new int[triangles.Length + spheres.Length];
			var aabbs = new AxisAlignedBoundingBox[tokens.Length];

			Parallel.For(0, triangles.Length, FillTriangles);
			Parallel.For(0, spheres.Length, FillSpheres);

			Thread.MemoryBarrier();

			void FillTriangles(int index)
			{
				var triangle = triangleList[index];
				triangles[index] = triangle;

				aabbs[index] = triangle.AABB;
				tokens[index] = index;
			}

			void FillSpheres(int index)
			{
				var sphere = sphereList[index];
				spheres[index] = sphere;

				aabbs[triangles.Length + index] = sphere.AABB;
				tokens[triangles.Length + index] = ~index;
			}

			triangleList = null; //Un-references large intermediate lists for GC
			sphereList = null;

			Program.commandsController.Log("Extracted scene");
			bvh = new BoundingVolumeHierarchy(this, aabbs, tokens);

			//Release resources
			CollectionPooler<Material, int>.dictionary.ReleaseObject(materialObjects);
			CollectionPooler<Object>.queue.ReleaseObject(frontier);
		}

		public readonly Camera camera;
		public readonly Cubemap cubemap;

		public readonly BoundingVolumeHierarchy bvh;

		readonly PressedTriangle[] triangles;
		readonly PressedSphere[] spheres;
		readonly Material[] materials;

		public int TriangleCount => triangles.Length;
		public int SphereCount => spheres.Length;
		public int MaterialCount => materials.Length;

		public readonly ReadOnlyCollection<PressedLight> lights;

		/// <summary>
		/// Returns the intersection status with one object of <paramref name="token"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Intersect(in Ray ray, int token, out Float2 uv)
		{
			if (token < 0)
			{
				ref PressedSphere sphere = ref spheres[~token];
				return sphere.GetIntersection(ray, out uv);
			}

			ref PressedTriangle triangle = ref triangles[token];
			return triangle.GetIntersection(ray, out uv);
		}

		/// <summary>
		/// Gets the normal of intersection with <paramref name="hit"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Float3 GetNormal(in Hit hit)
		{
			if (hit.token < 0)
			{
				ref PressedSphere sphere = ref spheres[~hit.token];
				return sphere.GetNormal(hit.uv);
			}

			ref PressedTriangle triangle = ref triangles[hit.token];
			return triangle.GetNormal(hit.uv);
		}

		/// <summary>
		/// Gets the texcoord of intersection with <paramref name="hit"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Float2 GetTexcoord(in Hit hit)
		{
			if (hit.token < 0) return hit.uv; //Sphere directly uses the uv as texcoord

			ref PressedTriangle triangle = ref triangles[hit.token];
			return triangle.GetTexcoord(hit.uv);
		}

		/// <summary>
		/// Gets the material of intersection with <paramref name="hit"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Material GetMaterial(in Hit hit)
		{
			int materialToken;

			if (hit.token < 0)
			{
				ref PressedSphere sphere = ref spheres[~hit.token];
				materialToken = sphere.materialToken;
			}
			else
			{
				ref PressedTriangle triangle = ref triangles[hit.token];
				materialToken = triangle.materialToken;
			}

			return materials[materialToken];
		}

		public void Write(DataWriter writer)
		{
			//TODO: Not writing camera, directional light, and skybox right now
		}
	}
}