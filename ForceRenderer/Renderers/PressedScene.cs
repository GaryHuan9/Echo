using System;
using System.Collections.Generic;
using CodeHelpers.ObjectPooling;
using CodeHelpers.Vectors;
using ForceRenderer.Objects;
using ForceRenderer.Scenes;
using Object = ForceRenderer.Objects.Object;

namespace ForceRenderer.Renderers
{
	/// <summary>
	/// A flattened out/pressed down record version of a scene for faster iteration.
	/// </summary>
	public class PressedScene
	{
		public PressedScene(Scene source)
		{
			this.source = source;

			List<SceneObject> objects = CollectionPooler<SceneObject>.list.GetObject();
			Queue<Object> frontier = CollectionPooler<Object>.queue.GetObject();

			//Find all scene objects
			frontier.Enqueue(source);

			while (frontier.Count > 0)
			{
				Object target = frontier.Dequeue();
				if (target != source && target is SceneObject sceneObject) objects.Add(sceneObject);

				Object.Children children = target.children;
				for (int i = 0; i < children.Count; i++) frontier.Enqueue(children[i]);
			}

			//Extract pressed data
			bundleCount = objects.Count;
			bundles = new Bundle[bundleCount];

			for (int i = 0; i < bundleCount; i++)
			{
				SceneObject sceneObject = objects[i];
				bundles[i] = new Bundle(sceneObject);
			}

			//Release
			CollectionPooler<SceneObject>.list.ReleaseObject(objects);
			CollectionPooler<Object>.queue.ReleaseObject(frontier);
		}

		public readonly Scene source;

		readonly int bundleCount;
		readonly Bundle[] bundles; //Contiguous chunks of data

		public float GetSignedDistance(Float3 point)
		{
			float distance = float.PositiveInfinity;

			for (int i = 0; i < bundleCount; i++)
			{
				Bundle bundle = bundles[i];

				Float3 transformed = bundle.transformation.Backward(point);
				distance = Math.Min(distance, bundle.sceneObject.SignedDistanceRaw(transformed));
			}

			return distance;
		}

		readonly struct Bundle
		{
			public Bundle(SceneObject sceneObject)
			{
				this.sceneObject = sceneObject;
				transformation = sceneObject.Transformation;
			}

			public readonly SceneObject sceneObject;
			public readonly Transformation transformation;
		}
	}
}