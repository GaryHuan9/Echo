using System;
using BenchmarkDotNet.Attributes;
using CodeHelpers.Mathematics;
using ForceRenderer;
using ForceRenderer.IO;
using ForceRenderer.Mathematics;
using ForceRenderer.Objects.SceneObjects;
using ForceRenderer.Rendering;

namespace IntrinsicsSIMD
{
	public class BenchmarkBVH
	{
		public BenchmarkBVH()
		{
			Scene scene = new Scene();
			Random random = new Random(42);

			// scene.children.Add(new MeshObject(new Mesh(@"C:\Users\MMXXXVIII\Things\CodingStuff\C#\ForceRenderer\ForceRenderer\Assets\Models\BlenderBMW\BlenderBMW.obj")));

			bvh = new PressedScene(scene).bvh;
			rays = new Ray[65536];

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

		//V0: 69us per 128 intersections = 540ns per intersection
		//V1:

		[Benchmark]
		public void GetIntersection()
		{
			for (int i = 0; i < rays.Length; i++) bvh.GetIntersection(rays[i]);
		}
	}

}