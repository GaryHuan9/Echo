using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CodeHelpers.ObjectPooling;
using CodeHelpers.Vectors;
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
			bundles = new Bundle[bundleCount];

			for (int i = 0; i < bundleCount; i++)
			{
				SceneObject sceneObject = objects[i];
				bundles[i] = new Bundle(i, sceneObject);
			}

			//Release
			CollectionPooler<SceneObject>.list.ReleaseObject(objects);
			CollectionPooler<Object>.queue.ReleaseObject(frontier);
		}

		public readonly Scene source;

		public readonly Camera camera;
		public readonly DirectionalLight directionalLight;

		readonly int bundleCount;
		readonly Bundle[] bundles; //Contiguous chunks of data; sorted by scene object hash code

		/// <summary>
		/// Gets the signed distance from <paramref name="point"/> to the scene.
		/// <paramref name="token"/> contains the token of the <see cref="SceneObject"/> that is the closest to <paramref name="point"/>.
		/// </summary>
		public float GetSignedDistance(Float3 point, out int token, int exclude = -1)
		{
			float distance = float.PositiveInfinity;
			token = -1;

			for (int i = 0; i < bundleCount; i++)
			{
				if (i == exclude) continue;
				Bundle bundle = bundles[i];

				Float3 transformed = bundle.transformation.Backward(point);
				float local = bundle.sceneObject.GetSignedDistanceRaw(transformed);

				if (local >= distance) continue;

				distance = local;
				token = i;
			}

			return distance;
		}


		/// <summary>
		/// Gets the signed distance from <paramref name="point"/> to the scene.
		/// </summary>
		public float GetSignedDistance(Float3 point, int exclude = -1) => GetSignedDistance(point, out int _, exclude);

		/// <summary>
		/// Gets the signed distance from <paramref name="point"/> to object with <paramref name="token"/>.
		/// </summary>
		public float GetSingleDistance(Float3 point, int token) => GetSingleDistance(point, bundles[token]);

		/// <inheritdoc cref="GetSingleDistance"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		static float GetSingleDistance(Float3 point, in Bundle bundle)
		{
			Float3 transformed = bundle.transformation.Backward(point);
			return bundle.sceneObject.GetSignedDistanceRaw(transformed);
		}

		/// <inheritdoc cref="GetNormal(CodeHelpers.Vectors.Float3)"/>
		/// <paramref name="token"/> contains the token of the object used to calculate normal.
		public Float3 GetNormal(Float3 point, out int token)
		{
			GetSignedDistance(point, out token);
			return GetNormal(point, token);
		}

		/// <summary>
		/// Gets the normal of the scene at <paramref name="point"/>.
		/// This value might be approximated using distance gradients or it might be exact.
		/// Your <see cref="SceneObject"/> implementation determines the calculation.
		/// </summary>
		public Float3 GetNormal(Float3 point) => GetNormal(point, out int _);

		/// <inheritdoc cref="GetNormal(CodeHelpers.Vectors.Float3)"/>
		/// The object with <paramref name="token"/> should be closest to <paramref name="point"/>,
		/// and it will be the only object tested for normal.
		public Float3 GetNormal(Float3 point, int token)
		{
			Bundle bundle = bundles[token];

			if (bundle.hasNormalImplementation)
			{
				Transformation transformation = bundle.transformation;
				Float3 transformed = transformation.Backward(point);

				Float3 normal = bundle.sceneObject.GetNormalRaw(transformed);
				return transformation.ForwardDirection(normal);
			}

			//No proper normal implementation, fallback to gradient approximation
			//Sample 6 distance values gradient using, 2 for each axis

			const float E = 1.2E-4f; //Epsilon value used for gradient offsets

			return new Float3
			(
				Sample(Float3.CreateX(E)) - Sample(Float3.CreateX(-E)),
				Sample(Float3.CreateY(E)) - Sample(Float3.CreateY(-E)),
				Sample(Float3.CreateZ(E)) - Sample(Float3.CreateZ(-E))
			).Normalized;

			float Sample(Float3 epsilon) => GetSingleDistance(point + epsilon, bundle);
		}

		readonly struct Bundle
		{
			public Bundle(int token, SceneObject sceneObject)
			{
				this.token = token;
				this.sceneObject = sceneObject;

				transformation = sceneObject.Transformation;
				hasNormalImplementation = true;

				try { sceneObject.GetNormalRaw(Float3.zero); }
				catch (NotSupportedException) { hasNormalImplementation = false; }
			}

			public readonly int token;
			public readonly SceneObject sceneObject;

			public readonly Transformation transformation;
			public readonly bool hasNormalImplementation; //Use enum flags?
		}
	}
}