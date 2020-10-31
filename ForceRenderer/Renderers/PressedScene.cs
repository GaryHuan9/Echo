using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CodeHelpers.Collections;
using CodeHelpers.ObjectPooling;
using CodeHelpers.Vectors;
using ForceRenderer.Objects;
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
				bundles[i] = new Bundle(i, sceneObject);
			}

			//Release
			CollectionPooler<SceneObject>.list.ReleaseObject(objects);
			CollectionPooler<Object>.queue.ReleaseObject(frontier);
		}

		public readonly Scene source;

		readonly int bundleCount;
		readonly Bundle[] bundles; //Contiguous chunks of data; sorted by scene object hash code

		/// <summary>
		/// Gets the signed distance from <paramref name="point"/> to the scene.
		/// <paramref name="token"/> contains the token of the <see cref="SceneObject"/> that is the closest to <paramref name="point"/>.
		/// </summary>
		public float GetSignedDistance(Float3 point, out int token)
		{
			float distance = float.PositiveInfinity;
			token = -1;

			for (int i = 0; i < bundleCount; i++)
			{
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
		public float GetSignedDistance(Float3 point) => GetSignedDistance(point, out int _);

		/// <summary>
		/// Gets the signed distance from <paramref name="point"/> to object with <paramref name="token"/>.
		/// </summary>
		public float GetSignedDistance(Float3 point, int token) => GetSignedDistance(point, bundles[token]);

		/// <inheritdoc cref="GetSignedDistance(CodeHelpers.Vectors.Float3,int)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		static float GetSignedDistance(Float3 point, in Bundle bundle)
		{
			Float3 transformed = bundle.transformation.Backward(point);
			return bundle.sceneObject.GetSignedDistanceRaw(transformed);
		}

		/// <inheritdoc cref="GetNormal(CodeHelpers.Vectors.Float3)"/>
		/// <paramref name="token"/> contains the token of the object used to calculate normal.
		public Float3 GetNormal(Float3 point, out int token)
		{
			float center = GetSignedDistance(point, out token);
			Bundle bundle = bundles[token];

			if (bundle.hasNormalImplementation)
			{
				Transformation transformation = bundle.transformation;
				Float3 transformed = transformation.Backward(point);

				Float3 normal = bundle.sceneObject.GetNormalRaw(transformed);
				return transformation.ForwardDirection(normal);
			}

			//No proper normal implementation, fallback to gradient approximation
			const float E = 1E-5f; //Epsilon value used for gradient offsets

			return new Float3
			(
				Sample(Float3.CreateX(E)) - Sample(Float3.CreateX(-E)),
				Sample(Float3.CreateY(E)) - Sample(Float3.CreateY(-E)),
				Sample(Float3.CreateZ(E)) - Sample(Float3.CreateZ(-E))
			).Normalized;

			float Sample(Float3 epsilon) => GetSignedDistance(point + epsilon, bundle) - center;
		}

		/// <summary>
		/// Gets the normal of the scene at <paramref name="point"/>.
		/// This value might be approximated using distance gradients or it might be exact.
		/// Your <see cref="SceneObject"/> implementation determines the calculation.
		/// </summary>
		public Float3 GetNormal(Float3 point) => GetNormal(point, out int _);

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