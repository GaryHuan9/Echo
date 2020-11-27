using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CodeHelpers;
using CodeHelpers.ObjectPooling;
using CodeHelpers.Vectors;
using ForceRenderer.Mathematics;
using ForceRenderer.Objects;
using ForceRenderer.Objects.SceneObjects;
using Object = ForceRenderer.Objects.Object;

namespace ForceRenderer.Renderers
{
	/// <summary>
	/// A flattened out/pressed down record version of a scene for fast iteration.
	/// </summary>
	public class PressedScene
	{
		public PressedScene(Scene source)
		{
			ExceptionHelper.InvalidIfNotMainThread();
			this.source = source;

			List<PressedTriangle> triangleList = CollectionPooler<PressedTriangle>.list.GetObject();
			List<PressedSphere> sphereList = CollectionPooler<PressedSphere>.list.GetObject();

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
						Material material = sceneObject.Material;

						if (!materialObjects.TryGetValue(material, out int materialToken))
						{
							materialToken = materialObjects.Count;
							materialObjects.Add(material, materialToken);
						}

						int triangleCount = triangleList.Count;

						triangleList.AddRange(sceneObject.ExtractTriangles(materialToken));
						sphereList.AddRange(sceneObject.ExtractSpheres(materialToken));

						for (int i = triangleCount; i < triangleList.Count; i++) totalArea += triangleList[i].Area;

						break;
					}
					case Camera value:
					{
						if (camera == null) camera = value;
						else Console.WriteLine($"Multiple {nameof(Camera)} found! Only the first one will be used.");

						break;
					}
					case DirectionalLight value:
					{
						if (directionalLight.direction == default) directionalLight = new PressedDirectionalLight(value);
						else Console.WriteLine($"Multiple {nameof(DirectionalLight)} found! Only the first one will be used.");

						break;
					}
				}

				Object.Children children = target.children;
				for (int i = 0; i < children.Count; i++) frontier.Enqueue(children[i]);
			}

			//Divide large triangles for better BVH space partitioning
			{
				const float DivideThresholdMultiplier = 4.8f;
				const int DivideMaxIteration = 3;

				int triangleCount = triangleList.Count;
				float threshold = (float)(totalArea / triangleCount * DivideThresholdMultiplier);

				for (int i = 0; i < triangleCount; i++)
				{
					PressedTriangle pressed = triangleList[i];

					Fragment(i, Math.Min(MathF.Log2(pressed.Area / threshold).Ceil(), DivideMaxIteration));

					void Fragment(int index, int iteration)
					{
						if (iteration <= 0) return;
						Span<PressedTriangle> divided = stackalloc PressedTriangle[4];

						triangleList[index].GetSubdivided(divided);
						int listCount = triangleList.Count;

						triangleList[index] = divided[0];
						triangleList.Add(divided[1]);
						triangleList.Add(divided[2]);
						triangleList.Add(divided[3]);

						if (--iteration == 0) return;

						Fragment(index, iteration);
						Fragment(listCount, iteration);
						Fragment(listCount + 1, iteration);
						Fragment(listCount + 2, iteration);
					}
				}
			}

			//Extract pressed data and construct BVH acceleration structure
			materials = (from pair in materialObjects
						 orderby pair.Value
						 select new PressedMaterial(pair.Key)).ToArray();

			triangles = new PressedTriangle[triangleList.Count];
			spheres = new PressedSphere[sphereList.Count];

			var aabbs = CollectionPooler<AxisAlignedBoundingBox>.list.GetObject();
			var indices = CollectionPooler<int>.list.GetObject();

			aabbs.Capacity = indices.Capacity = triangles.Length + spheres.Length;

			for (int i = 0; i < triangles.Length; i++)
			{
				var triangle = triangles[i] = triangleList[i];

				aabbs.Add(triangle.AABB);
				indices.Add(i);
			}

			for (int i = 0; i < spheres.Length; i++)
			{
				var sphere = spheres[i] = sphereList[i];

				aabbs.Add(sphere.AABB);
				indices.Add(~i);
			}

			bvh = new BoundingVolumeHierarchy(this, aabbs, indices);

			//Release
			CollectionPooler<PressedTriangle>.list.ReleaseObject(triangleList);
			CollectionPooler<PressedSphere>.list.ReleaseObject(sphereList);

			CollectionPooler<Material, int>.dictionary.ReleaseObject(materialObjects);
			CollectionPooler<Object>.queue.ReleaseObject(frontier);

			CollectionPooler<AxisAlignedBoundingBox>.list.ReleaseObject(aabbs);
			CollectionPooler<int>.list.ReleaseObject(indices);
		}

		public readonly Scene source;
		public readonly BoundingVolumeHierarchy bvh;

		public readonly Camera camera;
		public readonly PressedDirectionalLight directionalLight;

		readonly PressedMaterial[] materials;
		readonly PressedTriangle[] triangles;
		readonly PressedSphere[] spheres;

		public int MaterialCount => materials.Length;
		public int TriangleCount => triangles.Length;
		public int SphereCount => spheres.Length;

		/// <summary>
		/// Returns the <see cref="PressedMaterial"/> for object with <paramref name="token"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref PressedMaterial GetMaterial(int token)
		{
			int materialToken;

			if (token < 0)
			{
				ref PressedSphere sphere = ref spheres[~token];
				materialToken = sphere.materialToken;
			}
			else
			{
				ref PressedTriangle triangle = ref triangles[token];
				materialToken = triangle.materialToken;
			}

			return ref materials[materialToken];
		}

		/// <summary>
		/// Returns the intersection status with one object of <paramref name="token"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetIntersection(in Ray ray, int token, out Float2 uv)
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
		/// Gets the normal of <see cref="SceneObject"/> with <paramref name="token"/> at <paramref name="uv"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Float3 GetNormal(Float2 uv, int token)
		{
			if (token < 0)
			{
				ref PressedSphere sphere = ref spheres[~token];
				return sphere.GetNormal(uv);
			}

			ref PressedTriangle triangle = ref triangles[token];
			return triangle.GetNormal(uv);
		}
	}
}