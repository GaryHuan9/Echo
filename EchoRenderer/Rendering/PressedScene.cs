using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading;
using CodeHelpers.Diagnostics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Objects;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Textures.Cubemaps;
using Object = EchoRenderer.Objects.Object;

namespace EchoRenderer.Rendering
{
	/// <summary>
	/// A flattened out/pressed down record version of a scene for fast interactions.
	/// </summary>
	public class PressedScene
	{
		public PressedScene(Scene source)
		{
			this.source = source;
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
			rootPack = presser.PressPacks();    //Third pass create bvh

			materials = presser.materials.GetMapper(null); //Get default material mapper
			presser.materials.Press();                     //Press materials and mappers

			Program.commandsController?.Log("Pressed scene");
		}

		public readonly Scene source;
		public readonly ScenePresser presser;

		public readonly Camera camera;
		public readonly Cubemap cubemap;
		public readonly ReadOnlyCollection<PressedLight> lights;

		long intersections; //Intersection count

		public long Intersections => Interlocked.Read(ref intersections);

		readonly PressedPack rootPack;
		readonly MaterialPresser.Mapper materials;

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

		public bool GetIntersection(in Ray ray)
		{
			//TODO: We need a separate implementation that calculates intersection with any geometry (boolean true/false return)
			//TODO: This will significantly improve the performance of shadow rays since any intersection is enough to exit the calculation

			return GetIntersection(ray, out CalculatedHit hit);
		}

		public int GetIntersectionCost(in Ray ray)
		{
			float distance = float.PositiveInfinity;
			return rootPack.bvh.GetIntersectionCost(ray, ref distance);
		}

		public void ResetIntersectionCount() => Interlocked.Exchange(ref intersections, 0);
	}
}