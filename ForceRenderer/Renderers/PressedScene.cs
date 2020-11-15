using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.ObjectPooling;
using CodeHelpers.Vectors;
using ForceRenderer.Mathematics;
using ForceRenderer.Objects;
using ForceRenderer.Objects.Lights;
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

			List<TriangleObject> triangleObjects = CollectionPooler<TriangleObject>.list.GetObject();
			List<SphereObject> sphereObjects = CollectionPooler<SphereObject>.list.GetObject();

			List<Material> materialObjects = CollectionPooler<Material>.list.GetObject();
			Queue<Object> frontier = CollectionPooler<Object>.queue.GetObject();

			//Find all rendering-related objects
			frontier.Enqueue(source);

			while (frontier.Count > 0)
			{
				Object target = frontier.Dequeue();

				switch (target)
				{
					case TriangleObject value:
					{
						triangleObjects.Add(value);
						TryAddMaterial(value);

						break;
					}
					case SphereObject value:
					{
						sphereObjects.Add(value);
						TryAddMaterial(value);

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
						if (directionalLight == null) directionalLight = value;
						else Console.WriteLine($"Multiple {nameof(DirectionalLight)} found! Only the first one will be used.");

						break;
					}
				}

				Object.Children children = target.children;
				for (int i = 0; i < children.Count; i++) frontier.Enqueue(children[i]);

				void TryAddMaterial(SceneObject sceneObject)
				{
					Material material = sceneObject.Material;
					int index = materialObjects.BinarySearch(material, MaterialComparer.instance);

					if (index >= 0) return;
					materialObjects.Insert(~index, material);
				}
			}

			//Extract pressed data
			materialCount = materialObjects.Count;
			triangleCount = triangleObjects.Count;
			sphereCount = sphereObjects.Count;

			materials = new PressedMaterial[materialCount];
			triangles = new PressedTriangle[triangleCount];
			spheres = new PressedSphere[sphereCount];

			for (int i = 0; i < materialCount; i++) materials[i] = materialObjects[i].Pressed;

			for (int i = 0; i < triangleCount; i++)
			{
				TriangleObject triangle = triangleObjects[i];
				Material material = triangle.Material;

				triangles[i] = new PressedTriangle(triangle, materialObjects.BinarySearch(material, MaterialComparer.instance));
			}

			for (int i = 0; i < sphereCount; i++)
			{
				SphereObject sphere = sphereObjects[i];
				Material material = sphere.Material;

				spheres[i] = new PressedSphere(sphere, materialObjects.BinarySearch(material, MaterialComparer.instance));
			}

			//Release
			CollectionPooler<TriangleObject>.list.ReleaseObject(triangleObjects);
			CollectionPooler<SphereObject>.list.ReleaseObject(sphereObjects);

			CollectionPooler<Material>.list.ReleaseObject(materialObjects);
			CollectionPooler<Object>.queue.ReleaseObject(frontier);
		}

		public readonly Scene source;

		public readonly Camera camera;
		public readonly DirectionalLight directionalLight;

		public readonly int materialCount;
		public readonly int triangleCount;
		public readonly int sphereCount;

		//Contiguous chunks of data
		readonly PressedMaterial[] materials;
		readonly PressedTriangle[] triangles;
		readonly PressedSphere[] spheres;

		/// <summary>
		/// Returns the <see cref="PressedMaterial"/> for object with <paramref name="token"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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
		/// Returns the distance from scene intersection to ray origin.
		/// <paramref name="token"/> contains the token of the <see cref="SceneObject"/> that intersected with ray.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public float GetIntersection(in Ray ray, out int token)
		{
			float distance = float.PositiveInfinity;
			token = int.MaxValue;

			for (int i = 0; i < triangleCount; i++)
			{
				ref PressedTriangle triangle = ref triangles[i];
				float local = triangle.GetIntersection(ray);

				if (local >= distance) continue;

				distance = local;
				token = i;
			}

			for (int i = 0; i < sphereCount; i++)
			{
				ref PressedSphere sphere = ref spheres[i];
				float local = sphere.GetIntersection(ray);

				if (local >= distance) continue;

				distance = local;
				token = ~i;
			}

			return distance;
		}

		/// <summary>
		/// Returns the distance of the intersection between <paramref name="ray"/> and the scene.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public float GetIntersection(in Ray ray) => GetIntersection(ray, out int _);

		/// <summary>
		/// Gets the normal of <see cref="SceneObject"/> with <paramref name="token"/> at <paramref name="point"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public Float3 GetNormal(Float3 point, int token)
		{
			if (token < 0)
			{
				ref PressedSphere sphere = ref spheres[~token];
				return sphere.GetNormal(point);
			}

			ref PressedTriangle triangle = ref triangles[token];
			return triangle.GetNormal();
		}

		class MaterialComparer : IComparer<Material>
		{
			public readonly static MaterialComparer instance = new MaterialComparer();
			public int Compare(Material x, Material y) => (x?.GetHashCode() ?? 0).CompareTo(y?.GetHashCode() ?? 0);
		}
	}
}