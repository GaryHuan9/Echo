using System;
using BenchmarkDotNet.Attributes;
using CodeHelpers.Mathematics;
using ForceRenderer;
using ForceRenderer.IO;
using ForceRenderer.Mathematics;
using ForceRenderer.Objects.SceneObjects;
using ForceRenderer.Rendering;
using ForceRenderer.Rendering.Materials;

namespace IntrinsicsSIMD
{
	public class BenchmarkBVH
	{
		public BenchmarkBVH()
		{
			Scene scene = new Scene();
			Random random = new Random(42);

			Mesh mesh = new(@"C:\Users\MMXXXVIII\Things\CodingStuff\C#\ForceRenderer\ForceRenderer\Assets\Models\BlenderBMW\BlenderBMW.obj");
			scene.children.Add(new MeshObject(mesh, new Glossy()));

			bvh = new PressedScene(scene).bvh;
			rays = new Ray[1000];

			const float Radius = 6f;
			const float Height = 12f;

			for (int i = 0; i < rays.Length; i++)
			{
				var position = new Float3(Random() * Radius, Random() * Height, 0f).RotateXZ(Random() * 360f);
				rays[i] = new Ray(position, (new Float3(0f, 1.5f, 0f) - position).Normalized);
			}

			float Random() => (float)random.NextDouble();
		}

		readonly BoundingVolumeHierarchy bvh;
		readonly Ray[] rays;

		//V0: 903.5us per 1000 intersections
		//V1: 821.6us per 1000 intersections

		[Benchmark]
		public Hit GetIntersection()
		{
			Hit hit = default;

			for (int i = 0; i < rays.Length; i++) hit = bvh.GetIntersection(rays[i]);

			return hit;
		}

		// [Benchmark]
		public Hit GetIntersectionOld()
		{
			Hit hit = default;

			for (int i = 0; i < rays.Length; i++) hit = bvh.GetIntersectionOld(rays[i]);

			return hit;
		}
	}

}