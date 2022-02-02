using System;
using System.Collections.Generic;
using CodeHelpers.Mathematics;
using EchoRenderer.IO;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Scenic.GeometryObjects;

namespace EchoRenderer.Scenic.Examples
{
	public class SingleMaterialBall : StandardScene
	{
		public SingleMaterialBall() : base(new Glossy { Albedo = new Float3(0.78f, 0.76f, 0.79f), Smoothness = 0.74f })
		{
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime", (Float3)0.85f);

			var mesh = new Mesh("Assets/Models/BlenderMaterialBall/MaterialBall.zip");
			var materials = new MaterialLibrary("Assets/Models/BlenderMaterialBall/MaterialBall.mat");

			children.Add(new MeshObject(mesh, materials) { Position = new Float3(0f, 0f, -2.5f), Rotation = Float3.up * -75f, Scale = Float3.one * 2f });
		}
	}

	public class GridMaterialBall : Scene
	{
		//Benchmark Scene: 5.8 billion triangles
		//AMD 3900x 12C 24T: 5.7M ray/sec

		public GridMaterialBall()
		{
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime", (Float3)1.2f);

			var mesh = new Mesh("Assets/Models/BlenderMaterialBall/MaterialBall.zip");
			var materials = new MaterialLibrary("Assets/Models/BlenderMaterialBall/MaterialBall.mat");

			ObjectPack ball = new ObjectPack();
			Int3 gridSize = new Int3(10, 4, 7);

			ball.children.Add(new MeshObject(mesh, materials) { Rotation = Float3.up * -75f, Scale = (Float3)0.45f });

			Diffuse coreSource = (Diffuse)materials["Core"];
			Glass solidSource = (Glass)materials["Solid"];

			Dictionary<Int2, Material> cores = new Dictionary<Int2, Material>();
			Dictionary<int, Material> solids = new Dictionary<int, Material>();

			foreach (Int3 position in new EnumerableSpace3D(-gridSize, gridSize))
			{
				Float3 percent = Float3.InverseLerp(-gridSize, gridSize, position);

				throw new NotImplementedException();

				// Material core = cores.TryGetValue(position.YZ) ?? (cores[position.YZ] = new Diffuse { Albedo = Float3.Lerp(Float3.one, Float3.Lerp(coreSource.Albedo, Float3.zero, percent.z), percent.y) });
				// Material solid = solids.TryGetValue(position.x) ?? (solids[position.x] = new Glass { Albedo = solidSource.Albedo, IndexOfRefraction = solidSource.IndexOfRefraction, Roughness = percent.x });
				//
				// MaterialMapper mapper = new MaterialMapper { [coreSource] = core, [solidSource] = solid };
				// children.Add(new ObjectInstance(ball) { Position = position.XYZ, Mapper = mapper });
			}

			// children.Add(new Light { Intensity = Utilities.ToColor("#c9e2ff").XYZ, Rotation = new Float3(60f, 60f, 0f) });

			Camera camera = new Camera(100f) { Position = new Float3(-5f, 6f, -10f) };

			camera.LookAt(Float3.zero);
			children.Add(camera);
		}
	}
}