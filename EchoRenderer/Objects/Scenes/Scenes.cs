using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.IO;
using EchoRenderer.Objects.GeometryObjects;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Textures.Cubemaps;

namespace EchoRenderer.Objects.Scenes
{
	public class SingleBunny : StandardScene
	{
		public SingleBunny()
		{
			var mesh = new Mesh("Assets/Models/StanfordBunny/bunny.obj");
			var materials = new MaterialLibrary("Assets/Models/StanfordBunny/bunny.mat");

			children.Add(new MeshObject(mesh, materials) { Position = new Float3(0f, 0f, -3f), Rotation = new Float3(0f, 180f, 0f), Scale = (Float3)2.5f });
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

				children.Add(new SphereObject(material, radius) { Position = position });
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

	public class GridSpheres : Scene
	{
		public GridSpheres()
		{
			Cubemap = new SixSideCubemap("Assets/Cubemaps/OutsideDayTime");
			children.Add(new Camera(100f) { Position = new Float3(0f, 0f, -3f), Rotation = Float3.zero });

			const int Width = 10;
			const int Height = 4;

			for (float i = 0f; i <= Width; i++)
			{
				for (float j = 0f; j <= Height; j++)
				{
					// var material = new Glossy {Albedo = Float3.Lerp((Float3)0.9f, new Float3(0.867f, 0.267f, 0.298f), j / Height), Smoothness = i / Width};
					// var material = new Glass { Albedo = (Float3)0.9f, IndexOfRefraction = Scalars.Lerp(1.1f, 1.7f, j / Height), Roughness = i / Width };

					var material = new Matte();

					children.Add(new SphereObject(material, 0.45f) { Position = new Float3(i - Width / 2f, j - Height / 2f, 2f) });
				}
			}
		}
	}
}