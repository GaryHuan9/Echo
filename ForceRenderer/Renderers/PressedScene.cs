using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CodeHelpers;
using CodeHelpers.ObjectPooling;
using CodeHelpers.Vectors;
using ForceRenderer.Mathematics;
using ForceRenderer.Objects;
using ForceRenderer.Objects.Lights;
using ForceRenderer.Objects.SceneObjects;
using ForceRenderer.Scenes;
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

			List<SceneObject> objects = CollectionPooler<SceneObject>.list.GetObject();
			Queue<Object> frontier = CollectionPooler<Object>.queue.GetObject();

			//Find all scene objects
			frontier.Enqueue(source);

			while (frontier.Count > 0)
			{
				Object target = frontier.Dequeue();

				switch (target)
				{
					case SceneObject value:
					{
						objects.Add(value);
						value.OnPressed();

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
			}

			//Extract pressed data
			bundleCount = objects.Count;
			bundles = new PressedBundle[bundleCount];

			for (int i = 0; i < bundleCount; i++)
			{
				SceneObject sceneObject = objects[i];
				bundles[i] = new PressedBundle(i, sceneObject);
			}

			//Release
			CollectionPooler<SceneObject>.list.ReleaseObject(objects);
			CollectionPooler<Object>.queue.ReleaseObject(frontier);
		}

		public readonly Scene source;

		public readonly Camera camera;
		public readonly DirectionalLight directionalLight;

		readonly int bundleCount;
		readonly PressedBundle[] bundles; //Contiguous chunks of data; sorted by scene object hash code

		/// <summary>
		/// Returns pressed bundle for object with <paramref name="token"/>
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public ref PressedBundle GetPressedBundle(int token) => ref bundles[token];

		/// <summary>
		/// Returns the distance from scene intersection to ray origin.
		/// <paramref name="token"/> contains the token of the <see cref="SceneObject"/> that intersected with ray.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public float GetIntersection(in Ray ray, out int token)
		{
			float distance = float.PositiveInfinity;
			token = -1;

			for (int i = 0; i < bundleCount; i++)
			{
				ref PressedBundle bundle = ref GetPressedBundle(i);
				float local = GetSingleIntersection(ray, bundle);

				if (local >= distance) continue;

				distance = local;
				token = i;
			}

			return distance;
		}

		/// <summary>
		/// Returns the distance of the intersection between <paramref name="ray"/> and the scene.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public float GetIntersection(in Ray ray) => GetIntersection(ray, out int _);

		/// <summary>
		/// Returns the distance of the intersection between <paramref name="ray"/> and <paramref name="token"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public float GetSingleIntersection(in Ray ray, int token)
		{
			ref PressedBundle bundle = ref GetPressedBundle(token);
			return GetSingleIntersection(ray, bundle);
		}

		/// <inheritdoc cref="GetSingleIntersection"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		static float GetSingleIntersection(in Ray ray, in PressedBundle bundle)
		{
			Ray transformed = ray.TransformBackward(bundle.transformation);
			return bundle.sceneObject.GetRawIntersection(transformed);
		}

		/// <summary>
		/// Gets the normal of <see cref="SceneObject"/> with <paramref name="token"/> at <paramref name="point"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public Float3 GetNormal(Float3 point, int token)
		{
			ref PressedBundle bundle = ref GetPressedBundle(token);

			Transformation transformation = bundle.transformation;
			Float3 transformed = transformation.Backward(point);

			Float3 normal = bundle.sceneObject.GetRawNormal(transformed);
			return transformation.ForwardDirection(normal);
		}
	}
}