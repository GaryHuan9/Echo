using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Objects;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Textures;
using EchoRenderer.Textures.Cubemaps;
using Object = EchoRenderer.Objects.Object;

namespace EchoRenderer.Rendering
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

			materials = presser.materials.GetMapper(null); //Get default material mapper
			presser.materials.Press();                     //Press materials and mappers

			Program.commandsController?.Log("Pressed scene");
		}

		public readonly Camera camera;
		public readonly Cubemap cubemap;
		public readonly ReadOnlyCollection<PressedLight> lights;

		public GeometryCounts InstancedCounts => presser.root.InstancedCounts;
		public GeometryCounts UniqueCounts => presser.root.UniqueCounts;

		public int MaterialCount => presser.materials.Count;
		public long Intersections => Interlocked.Read(ref intersections);

		long intersections; //Intersection count

		readonly ScenePresser presser;
		readonly PressedPack rootPack;
		readonly MaterialPresser.Mapper materials;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool GetIntersection(in Ray ray, out CalculatedHit calculated)
		{
			Hit hit = new Hit {distance = float.PositiveInfinity};

			rootPack.bvh.GetIntersection(ray, ref hit);
			Interlocked.Increment(ref intersections);

			if (float.IsInfinity(hit.distance))
			{
				Unsafe.SkipInit(out calculated);
				return false;
			}

			PressedPack pack;
			MaterialPresser.Mapper mapper;

			if (hit.instance == null)
			{
				pack = rootPack;
				mapper = materials;
				pack.GetNormal(ref hit);
			}
			else
			{
				pack = hit.instance.pack;
				mapper = hit.instance.materials;
			}

			calculated = pack.CreateHit(hit, ray, mapper);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool GetIntersection(in Ray ray)
		{
			float distance = float.PositiveInfinity;

			rootPack.bvh.GetIntersection(ray, ref distance);
			Interlocked.Increment(ref intersections);

			return float.IsFinite(distance);
		}

		public int GetIntersectionCost(in Ray ray)
		{
			float distance = float.PositiveInfinity;
			return rootPack.bvh.GetIntersectionCost(ray, ref distance);
		}
	}
}