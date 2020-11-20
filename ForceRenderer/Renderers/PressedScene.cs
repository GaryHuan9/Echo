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

						sceneObject.Press(triangleList, sphereList, materialToken);
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

			//Extract pressed data
			materials = (from pair in materialObjects
						 orderby pair.Value
						 select new PressedMaterial(pair.Key)).ToArray();

			triangles = triangleList.ToArray();
			spheres = sphereList.ToArray();

			//Release
			CollectionPooler<PressedTriangle>.list.ReleaseObject(triangleList);
			CollectionPooler<PressedSphere>.list.ReleaseObject(sphereList);

			CollectionPooler<Material, int>.dictionary.ReleaseObject(materialObjects);
			CollectionPooler<Object>.queue.ReleaseObject(frontier);
		}

		public readonly Scene source;
		public readonly Camera camera;

		public readonly PressedDirectionalLight directionalLight;

		//Contiguous chunks of data
		readonly PressedMaterial[] materials;
		readonly PressedTriangle[] triangles;
		readonly PressedSphere[] spheres;

		public int MaterialCount => materials.Length;
		public int TriangleCount => triangles.Length;
		public int SphereCount => spheres.Length;

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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetIntersection(in Ray ray, out int token)
		{
			float distance = float.PositiveInfinity;
			token = int.MaxValue;

			for (int i = 0; i < triangles.Length; i++)
			{
				ref PressedTriangle triangle = ref triangles[i];
				float local = triangle.GetIntersection(ray);

				if (local >= distance) continue;

				distance = local;
				token = i;
			}

			for (int i = 0; i < spheres.Length; i++)
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
		/// Returns the intersection status with one object of <paramref name="token"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetIntersection(in Ray ray, int token)
		{
			if (token < 0)
			{
				ref PressedSphere sphere = ref spheres[~token];
				return sphere.GetIntersection(ray);
			}

			ref PressedTriangle triangle = ref triangles[token];
			return triangle.GetIntersection(ray);
		}

		/// <summary>
		/// Gets the normal of <see cref="SceneObject"/> with <paramref name="token"/> at <paramref name="point"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
	}
}