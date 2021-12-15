using System;
using System.Collections.Generic;
using System.Threading;
using CodeHelpers.Diagnostics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Objects;
using EchoRenderer.Objects.Lights;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.Profiles;
using EchoRenderer.Textures.Cubemaps;
using Object = EchoRenderer.Objects.Object;

namespace EchoRenderer.Rendering
{
	/// <summary>
	/// A flattened out/pressed down record version of a scene for fast interactions.
	/// </summary>
	public class PressedScene
	{
		public PressedScene(Scene source, ScenePressProfile profile)
		{
			this.source = source;
			cubemap = source.Cubemap;

			var lightsList = new List<Light>();

			//Gather important objects
			foreach (Object child in source.LoopChildren(true))
			{
				switch (child)
				{
					case Camera value:
					{
						if (camera == null) camera = value;
						else DebugHelper.Log($"Multiple {nameof(Camera)} found! Only the first one will be used.");

						break;
					}
					case Light value:
					{
						lightsList.Add(value);
						break;
					}
				}

				if (child.Scale.MinComponent <= 0f) throw new Exception($"Cannot have non-positive scales! '{child.Scale}'");
			}

			lights = lightsList.ToArray();
			presser = new ScenePresser(source, profile);

			//Create root instance and press materials
			rootInstance = new PressedInstance(presser, source);
			presser.materials.Press();

			//Press lights
			foreach (Light light in lights) light.Press(this);

			DebugHelper.Log("Pressed scene");
		}

		public readonly Scene source;
		public readonly ScenePresser presser;

		public readonly Camera camera;
		public readonly Cubemap cubemap;
		readonly Light[] lights;

		long _traceCount; //Intersection count

		public ReadOnlySpan<Light> Lights => lights;

		public long TraceCount => Interlocked.Read(ref _traceCount);

		readonly PressedInstance rootInstance;

		/// <summary>
		/// Processes the <paramref name="query"/> and returns whether it intersected with something.
		/// </summary>
		public bool Trace(ref TraceQuery query)
		{
			float original = query.distance;

			rootInstance.TraceRoot(ref query);
			Interlocked.Increment(ref _traceCount);
			return query.distance < original;
		}

		/// <summary>
		/// Processes the <paramref name="query"/> and returns whether it is occluded by something.
		/// </summary>
		public bool Occlude(ref OccludeQuery query)
		{
			//TODO: We need a separate implementation that calculates intersection with any geometry (occlusion: boolean true/false return)
			//TODO: This will significantly improve the performance of shadow rays since any intersection is enough to exit the calculation

			var traceQuery = new TraceQuery(query.ray, query.travel, query.ignore);
			return Trace(ref traceQuery) && traceQuery.distance < query.travel;
		}

		/// <summary>
		/// Begins interacting with the result of <paramref name="query"/> by creating and
		/// returning an <see cref="Interaction"/> and outputting <paramref name="material"/>.
		/// </summary>
		public Interaction Interact(in TraceQuery query, out Material material)
		{
			query.AssertHit();
			ref readonly var token = ref query.token;

			var instance = token.InstanceCount == 0 ? rootInstance : presser.GetPressedPackInstance(token.FinalInstanceId);
			return instance.pack.Interact(query, presser, instance, out material);
		}

		/// <summary>
		/// Returns the approximated cost of computing a <see cref="TraceQuery"/> with <see cref="Trace"/>.
		/// </summary>
		public int TraceCost(in Ray ray)
		{
			float distance = float.PositiveInfinity;
			return rootInstance.TraceCost(ray, ref distance);
		}

		public void ResetIntersectionCount() => Interlocked.Exchange(ref _traceCount, 0);
	}
}