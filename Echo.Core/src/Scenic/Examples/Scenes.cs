﻿using CodeHelpers;
using CodeHelpers.Mathematics;
using CodeHelpers.Mathematics.Enumerable;
using CodeHelpers.Packed;
using Echo.Core.Evaluation.Materials;
using Echo.Core.InOut;
using Echo.Core.Scenic.Geometries;
using Echo.Core.Scenic.Instancing;
using Echo.Core.Scenic.Lights;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Directional;
using Echo.Core.Textures.Grid;
using System;

namespace Echo.Core.Scenic.Examples;

public class SingleBunny : StandardScene
{
	public SingleBunny()
	{
		var mesh = new Mesh("ext/Scenes/SingleBunny/bunny.obj");

		Pure blue = (Pure)new RGBA128(0.0250f, 0.1416f, 0.3736f);

		var material0 = new Matte { Albedo = (Pure)new RGBA128(1f, 0.68f, 0.16f, 1f) };
		var material1 = new Matte { Albedo = blue };
		var material2 = new Emissive { Albedo = Texture.white };
		var material3 = new Mirror { Albedo = (Pure)new RGBA128(0.75f) };

		children.Add(new AmbientLight { Texture = new CylindricalTexture { Texture = TextureGrid.Load<RGB128>("ext/Scenes/SingleBunny/UlmerMuenster.jpg") } });

		children.Add(new MeshEntity { Mesh = mesh, Material = material0, Position = new Float3(0f, 0f, -3f), Rotation = new Float3(0f, 180f, 0f), Scale = (Float3)2.5f });

		children.Add(new SphereEntity { Material = material3, Radius = 1f, Position = new Float3(-3f, 1f, -2f) });

		children.Add(new PlaneEntity { Material = material2, Size = Float2.One * 2f, Position = new Float3(4f, 1f, -2f), Rotation = new Float3(0f, 0f, 90f) });
		children.Add(new PlaneEntity { Material = material2, Size = Float2.One * 2f, Position = new Float3(-5f, 1f, -3f), Rotation = new Float3(0f, 0f, 90f) });

		children.Add(new PointLight { Intensity = new RGB128(20f, 10f, 10f), Position = new Float3(2f, 2f, -6f) });
		children.Add(new PointLight { Intensity = new RGB128(10f, 10f, 10f), Position = new Float3(-3f, 3f, -4f) });
	}
}

public class MirrorBox : StandardScene
{
	public MirrorBox()
	{
		var green = new Matte { Albedo = (Pure)RGBA128.Parse("0x00CB21") };
		var red = new Matte { Albedo = (Pure)RGBA128.Parse("0xCB0021") };
		var blue = new Matte { Albedo = (Pure)RGBA128.Parse("0x0021CB") };
		var white = new Matte { Albedo = (Pure)RGBA128.Parse("0xEEEEF2") };

		var cullable = new Cullable { Base = white };
		var light = new Emissive { Albedo = (Pure)RGBA128.Parse("0xFFFAF4") };

		var matMirror = new Mirror { Albedo = (Pure)new RGBA128(0.75f) };

		const float Width = 10f;
		const float Half = Width / 2f;
		const float Size = Half / 5f * 3f;

		children.Add(new PlaneEntity { Material = matMirror, Size = (Float2)Width, Position = Float3.Zero, Rotation = Float3.Zero });                                //Floor
		children.Add(new PlaneEntity { Material = matMirror, Size = (Float2)Width, Position = new Float3(0f, Width, 0f), Rotation = new Float3(180f, 0f, 0f) });     //Roof
		children.Add(new PlaneEntity { Material = matMirror, Size = (Float2)Width, Position = new Float3(0f, Half, Half), Rotation = new Float3(-90f, 0f, 0f) });     //Back
																																									  //children.Add(new PlaneEntity { Material = cullable, Size = (Float2)Width, Position = new Float3(0f, Half, -Half), Rotation = new Float3(90f, 0f, 0f) }); //Front

		children.Add(new PlaneEntity { Material = matMirror, Size = (Float2)Width, Position = new Float3(Half, Half, 0f), Rotation = new Float3(0f, 0f, 90f) }); //Right
		children.Add(new PlaneEntity { Material = matMirror, Size = (Float2)Width, Position = new Float3(-Half, Half, 0f), Rotation = new Float3(0f, 0f, -90f) }); //Left
		children.Add(new PlaneEntity { Material = light, Size = (Float2)Half, Position = new Float3(0f, Width - 0.01f, 0f), Rotation = Float3.Zero });       //Light

		children.Add(new BoxEntity { Material = white, Size = (Float3)Size, Position = new Float3(Size / 1.5f, Size / 2f, -Size / 1.5f), Rotation = Float3.Up * 21f });
		children.Add(new BoxEntity { Material = white, Size = new Float3(Size, Size * 2f, Size), Position = new Float3(-Size / 1.5f, Size, Size / 1.5f), Rotation = Float3.Up * -21f });

		Camera camera = new Camera(42f) { Position = new Float3(0f, Half, -Half) };

		float radian = Scalars.ToRadians(camera.FieldOfView / 2f);
		camera.Position += Float3.Backward * (Half / MathF.Tan(radian));

		children.Add(camera); //Position: (0, 5, -18.025444)
	}
}

public class RandomSpheres : StandardScene
{
	public RandomSpheres(int count)
	{
		AddSpheres(new MinMax(0f, 7f), new MinMax(0.4f, 0.7f), count);
		AddSpheres(new MinMax(0f, 7f), new MinMax(0.1f, 0.2f), count * 3);
	}

	public RandomSpheres() : this(120) { }

	void AddSpheres(MinMax positionRange, MinMax radiusRange, int count)
	{
		for (int i = 0; i < count; i++)
		{
			//Orientation
			float radius;
			Float3 position;

			do
			{
				radius = radiusRange.RandomValue;
				position = new Float3(positionRange.RandomValue, radius, 0f).RotateXZ(RandomHelper.Range(360f));
			}
			while (IntersectingOthers(radius, position));

			//Material
			Float3 color = new Float3((float)RandomHelper.Value, (float)RandomHelper.Value, (float)RandomHelper.Value);

			bool metal = RandomHelper.Value < 0.3d;
			bool emissive = RandomHelper.Value < 0.05d;

			Material material;

			// if (metal) material = new Glossy { Albedo = color, Smoothness = (float)RandomHelper.Value / 2f + 0.5f };
			// else if (emissive) material = new Emissive { Emission = color / color.MaxComponent * 3f };
			// else material = new Diffuse { Albedo = color };

			material = new Matte();

			children.Add(new SphereEntity { Material = material, Radius = radius, Position = position });
		}
	}

	bool IntersectingOthers(float radius, Float3 position)
	{
		for (int i = 0; i < children.Count; i++)
		{
			var sphere = children[i] as SphereEntity;
			if (sphere == null) continue;

			float distance = sphere.Radius + radius;
			if ((sphere.Position - position).SquaredMagnitude <= distance * distance) return true;
		}

		return false;
	}
}

public class GridSpheres : Scene
{
	public GridSpheres()
	{
		children.Add(new AmbientLight { Texture = new Cubemap("Assets/Cubemaps/OutsideDayTime") });
		children.Add(new Camera(100f) { Position = new Float3(0f, 0f, -3f), Rotation = Float3.Zero });

		const int Width = 10;
		const int Height = 4;

		for (float i = 0f; i <= Width; i++)
		{
			for (float j = 0f; j <= Height; j++)
			{
				// var material = new Glossy {Albedo = Float3.Lerp((Float3)0.9f, new Float3(0.867f, 0.267f, 0.298f), j / Height), Smoothness = i / Width};
				// var material = new Glass { Albedo = (Float3)0.9f, IndexOfRefraction = Scalars.Lerp(1.1f, 1.7f, j / Height), Roughness = i / Width };

				var material = new Matte();

				children.Add(new SphereEntity { Material = material, Radius = 0.45f, Position = new Float3(i - Width / 2f, j - Height / 2f, 2f) });
			}
		}
	}
}

public class TestInstancing : Scene
{
	public TestInstancing()
	{
		var mesh = new Mesh("Assets/Models/StanfordBunny/lowPoly.obj");
		var material0 = new Matte { Albedo = Texture.white };
		var material1 = new Matte { Albedo = Texture.normal };

		// var mesh = new Mesh("Assets/Models/BlenderMaterialBall/MaterialBall.zip");
		// var materials = new MaterialLibrary("Assets/Models/BlenderMaterialBall/MaterialBall.mat");

		EntityPack bunny = new EntityPack();
		EntityPack bunnyWall = new EntityPack();

		bunny.children.Add(new MeshEntity { Mesh = mesh, Material = material0, Rotation = new Float3(0f, 180f, 0f), Scale = (Float3)0.7f });
		bunny.children.Add(new SphereEntity { Material = material1, Radius = 0.1f, Position = new Float3(-0.3f, 0.2f, -0.3f) });

		foreach (Int2 offset in new EnumerableSpace2D(new Int2(-8, -5), new Int2(8, 5)))
		{
			bunnyWall.children.Add(new PackInstance { EntityPack = bunny, Position = offset.XY_ });
		}

		for (int z = 0; z < 4; z++)
		{
			children.Add(new PackInstance { EntityPack = bunnyWall, Position = new Float3(0f, 0f, z * 6f), Rotation = new Float3(0f, -20f * (z + 1f), 0f), Scale = (Float3)(z + 1f) });
		}

		bunnyWall.children.Add(new PlaneEntity { Material = material0, Position = new Float3(1f, -1f, 0f), Rotation = new Float3(-90f, -10f, 0f) });
		// bunnyWall.children.Add(new PackInstance{EntityPack = bunnyWall}); //Tests recursive instancing

		children.Add(new BoxEntity { Material = material0, Size = Float3.One });
		children.Add(new PlaneEntity { Material = material1, Size = Float2.One * 0.9f, Position = new Float3(-1.1f, -0.4f, 0.3f), Rotation = new Float3(-70f, 20f, 30f) });

		children.Add(new AmbientLight { Texture = new Cubemap("Assets/Cubemaps/OutsideDayTime") { Tint = Tint.Scale(new RGBA128(1.5f)) } });

		var camera = new Camera(110f) { Position = new Float3(4f, 27f, -25f) };

		camera.LookAt(Float3.Zero);
		children.Add(camera);
	}
}