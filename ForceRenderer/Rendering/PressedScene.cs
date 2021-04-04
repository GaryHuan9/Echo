using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using ForceRenderer.Mathematics.Intersections;
using ForceRenderer.Objects;
using ForceRenderer.Objects.Scenes;
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

			List<PressedLight> lightsList = new List<PressedLight>();

			//First pass gather important objects
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
						lightsList.Add(new PressedLight(value));
						break;
					}
				}

				if (child.Scale.MinComponent <= 0f) throw new Exception($"Cannot have non-positive scales! '{child.Scale}'");
			}

			lights = new ReadOnlyCollection<PressedLight>(lightsList);

			presser = new ScenePresser(source); //Second pass create presser
			rootPack = presser.PressPacks();    //Third pass create bounding volume hierarchies

			presser.materials.Press(); //Press materials
			Program.commandsController.Log("Pressed scene");
		}

		public readonly Camera camera;
		public readonly Cubemap cubemap;
		public readonly ReadOnlyCollection<PressedLight> lights;

		public GeometryCounts InstancedCounts => presser.root.InstancedCounts;
		public GeometryCounts UniqueCounts => presser.root.UniqueCounts;

		public int MaterialCount => presser.materials.Length;
		public long IntersectionPerformed => Interlocked.Read(ref intersectionPerformed);

		long intersectionPerformed;

		readonly ScenePresser presser;
		readonly PressedPack rootPack;

		public bool GetIntersection(in Ray ray, out CalculatedHit calculated)
		{
			Hit hit = new Hit();
			PressedPack pack;

			hit.distance = float.PositiveInfinity;

			rootPack.bvh.GetIntersection(ray, ref hit);
			Interlocked.Increment(ref intersectionPerformed);

			if (float.IsInfinity(hit.distance))
			{
				calculated = default;
				return false;
			}

			if (hit.instance == null)
			{
				pack = rootPack;
				pack.GetNormal(ref hit);
			}
			else pack = hit.instance.pack;

			calculated = pack.CreateHit(hit, ray);
			return true;
		}

		public int GetIntersectionCost(in Ray ray)
		{
			float distance = float.PositiveInfinity;
			return rootPack.bvh.GetIntersectionCost(ray, ref distance);
		}
	}
}