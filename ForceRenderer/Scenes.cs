using System;
using CodeHelpers;
using CodeHelpers.Vectors;
using ForceRenderer.IO;
using ForceRenderer.Objects;
using ForceRenderer.Objects.SceneObjects;

namespace ForceRenderer
{
	public class StandardScene : Scene
	{
		public StandardScene(Material ground = null)
		{
			ground ??= new Material {Albedo = (Float3)0.75f, Specular = (Float3)0.03f, Smoothness = 0.11f};
			PlacePlane(Float3.zero, Float3.up, new Float2(12f, 8f), ground);

			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideSea");
			children.Add(new Camera(110f) {Position = new Float3(0f, 3f, -6f), Rotation = new Float3(30f, 0f, 0f)});
		}
	}

	public class BunnyScene : StandardScene
	{
		public BunnyScene()
		{
			Mesh bunny = new Mesh("Assets/Models/StanfordBunny/bunnyLP.obj");

			Material gold = new Material {Albedo = new Float3(0.346f, 0.314f, 0.0903f), Specular = new Float3(0.797f, 0.724f, 0.208f), Smoothness = 0.78f};
			children.Add(new MeshObject(gold, bunny) {Position = new Float3(0f, 0f, -3f), Rotation = new Float3(0f, 180f, 0f), Scale = (Float3)2.5f});
		}
	}

	public class KunaiScene : StandardScene
	{
		public KunaiScene()
		{
			Mesh kunai = new Mesh("Assets/Models/Kunai/wraith_kunai.obj");

			Material chrome = new Material {Albedo = new Float3(0.4f, 0.4f, 0.4f), Specular = new Float3(0.775f, 0.775f, 0.775f), Smoothness = 0.92f};
			children.Add(new MeshObject(chrome, kunai) {Position = new Float3(0f, 0f, -3f), Rotation = new Float3(0f, 90f, 0f), Scale = (Float3)2.5f});
		}
	}

	public class SingleBMWScene : StandardScene
	{
		public SingleBMWScene() : base(new Material {Albedo = (Float3)0.05f, Specular = (Float3)0.88f, Smoothness = 0.78f})
		{
			Mesh bmw = new Mesh("Assets/Models/BlenderBMW/BlenderBMW.obj");
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime");

			Material dark = new Material {Albedo = new Float3(0.1f, 0.1f, 0.1f), Specular = Float3.half, Smoothness = 0.9f};
			children.Add(new MeshObject(dark, bmw) {Position = Float3.zero, Rotation = new Float3(0f, -65f, 0f), Scale = (Float3)1.4f});
		}
	}

	public class MultipleBMWScene : StandardScene
	{
		public MultipleBMWScene() : base(new Material {Albedo = (Float3)0.05f, Specular = (Float3)0.88f, Smoothness = 0.78f})
		{
			Mesh bmw = new Mesh("Assets/Models/BlenderBMW/BlenderBMW.obj");
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime");

			MinMaxInt range = new MinMaxInt(-3, 1);

			foreach (int index in range.Loop())
			{
				Material material = new Material {Albedo = Float3.one, Specular = (Float3)range.InverseLerp(index), Smoothness = 0.85f};
				Float3 position = new Float3(2.8f, 0f, -0.8f) * index + new Float3(1.7f, 0f, 0.2f);

				children.Add(new MeshObject(material, bmw) {Position = position, Rotation = new Float3(0f, -60f, 0f)});
			}
		}
	}

	public class RandomSpheresScene : StandardScene
	{
		public RandomSpheresScene(int count)
		{
			MinMax radiusRange = new MinMax(0.22f, 0.58f);
			MinMax positionRange = new MinMax(0f, 4.5f);

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
				Float3 bright = new Float3(RandomHelper.Range(3f, 8f), RandomHelper.Range(3f, 8f), RandomHelper.Range(3f, 8f));

				bool metal = RandomHelper.Value < 0.5d;
				bool emissive = RandomHelper.Value < 0.1d;

				Material material = new Material
									{
										Albedo = color,
										Specular = metal ? color : Float3.one * 0.05f,
										Emission = emissive ? bright : Float3.zero,
										Smoothness = (float)RandomHelper.Value / 2f + (metal ? 0.5f : 0f)
									};

				children.Add(new SphereObject(material, radius) {Position = position});
			}
		}

		bool IntersectingOthers(float radius, Float3 position)
		{
			for (int i = 0; i < children.Count; i++)
			{
				var sphere = children[i] as SphereObject;
				if (sphere == null) continue;

				float distance = sphere.Radius + radius;
				if ((sphere.Position - position).SquaredMagnitude <= distance * distance) return true;
			}

			return false;
		}
	}

	public class GridSpheresScene : Scene
	{
		public GridSpheresScene()
		{
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideSea");
			children.Add(new Camera(100f) {Position = new Float3(0f, 0f, -3f), Rotation = Float3.zero});

			const int SmoothnessLevel = 10;
			const int SpecularLevel = 4;

			for (float i = 0f; i <= SmoothnessLevel; i++)
			{
				for (float j = 0f; j <= SpecularLevel; j++)
				{
					Material material = new Material {Specular = Float3.Lerp(Float3.one, new Float3(0.867f, 0.267f, 0.298f), j / SpecularLevel), Smoothness = i / SmoothnessLevel};
					children.Add(new SphereObject(material, 0.45f) {Position = new Float3(i - SmoothnessLevel / 2f, j - SpecularLevel / 2f, 2f)});
				}
			}
		}
	}
}