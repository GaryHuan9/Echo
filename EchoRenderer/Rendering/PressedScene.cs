using System;
using System.Collections.Generic;
using System.Threading;
using CodeHelpers.Diagnostics;
using EchoRenderer.Mathematics.Accelerators;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Objects;
using EchoRenderer.Objects.Lights;
using EchoRenderer.Objects.Scenes;
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
			rootInstance = new PressedPackInstance(presser, source);
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

		long _intersections; //Intersection count

		public ReadOnlySpan<Light> Lights => lights;

		public long Intersections => Interlocked.Read(ref _intersections);

		readonly PressedPackInstance rootInstance;

		/// <inheritdoc cref="TraceAccelerator.GetIntersection(ref HitQuery)"/>
		public bool GetIntersection(ref HitQuery query)
		{
			rootInstance.GetIntersectionRoot(ref query);
			Interlocked.Increment(ref _intersections);

			bool hit = query.Hit;

			if (hit)
			{
				var instance = presser.GetPressedPackInstance(query.token.instance);
				instance.pack.FillShading(ref query, instance);
			}

			return hit;
		}

		public bool GetIntersection(in Ray ray)
		{
			//TODO: We need a separate implementation that calculates intersection with any geometry (occlusion: boolean true/false return)
			//TODO: This will significantly improve the performance of shadow rays since any intersection is enough to exit the calculation

			HitQuery query = ray;
			return GetIntersection(ref query);
		}

		/// <inheritdoc cref="TraceAccelerator.GetIntersectionCost"/>
		public int GetIntersectionCost(in Ray ray)
		{
			float distance = float.PositiveInfinity;
			return rootInstance.GetIntersectionCost(ray, ref distance);
		}

		public void ResetIntersectionCount() => Interlocked.Exchange(ref _intersections, 0);
	}
}