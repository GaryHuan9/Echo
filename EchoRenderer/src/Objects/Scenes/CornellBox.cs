using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Objects.GeometryObjects;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Objects.Scenes
{
	public class CornellBox : Scene
	{
		public CornellBox()
		{
			Diffuse green = new Diffuse {Albedo = Utilities.ToColor("00CB21").XYZ};
			Diffuse red = new Diffuse {Albedo = Utilities.ToColor("CB0021").XYZ};
			Diffuse blue = new Diffuse {Albedo = Utilities.ToColor("0021CB").XYZ};

			Diffuse white = new Diffuse {Albedo = Utilities.ToColor("EEEEF2").XYZ};
			Emissive light = new Emissive {Emission = Utilities.ToColor("#FFFAF4").XYZ * 3.4f};

			const float Width = 10f;
			const float Half = Width / 2f;
			const float Size = Half / 5f * 3f;

			children.Add(new PlaneObject(white, (Float2)Width) {Position = Float3.zero, Rotation = Float3.zero});                             //Floor
			children.Add(new PlaneObject(white, (Float2)Width) {Position = new Float3(0f, Width, 0f), Rotation = new Float3(180f, 0f, 0f)});  //Roof
			children.Add(new PlaneObject(blue, (Float2)Width) {Position = new Float3(0f, Half, Half), Rotation = new Float3(-90f, 0f, 0f)});  //Back
			children.Add(new PlaneObject(white, (Float2)Width) {Position = new Float3(0f, Half, -Half), Rotation = new Float3(90f, 0f, 0f)}); //Front

			children.Add(new PlaneObject(green, (Float2)Width) {Position = new Float3(Half, Half, 0f), Rotation = new Float3(0f, 0f, 90f)});        //Right
			children.Add(new PlaneObject(red, (Float2)Width) {Position = new Float3(-Half, Half, 0f), Rotation = new Float3(0f, 0f, -90f)});        //Left
			children.Add(new PlaneObject(light, (Float2)Half) {Position = new Float3(0f, Width - 0.01f, 0f), Rotation = new Float3(180f, 0f, 0f)}); //Light

			children.Add(new BoxObject(white, new Float3(Size, Size, Size)) {Position = new Float3(Size / 1.5f, Size / 2f, -Size / 1.5f), Rotation = Float3.up * 21f});
			children.Add(new BoxObject(white, new Float3(Size, Size * 2f, Size)) {Position = new Float3(-Size / 1.5f, Size, Size / 1.5f), Rotation = Float3.up * -21f});

			const float Radius = 0.4f;
			const int Count = 3;

			Float3 ballWhite = Utilities.ToColor("F7F7FD").XYZ;

			for (int i = -Count; i <= Count; i++)
			{
				float percent = Scalars.InverseLerp(-Count, Count, (float)i);
				Float4 position = new Float4(i * Radius * 2.1f, Width - Radius, Radius - Half, Half - Radius);

				children.Add(new SphereObject(new Glass {Albedo = ballWhite, IndexOfRefraction = 1.5f, Roughness = percent}, Radius) {Position = position.ZYX});
				children.Add(new SphereObject(new Glass {Albedo = ballWhite, IndexOfRefraction = Scalars.Lerp(1.1f, 2.1f, percent)}, Radius) {Position = position.XYW});
				children.Add(new SphereObject(new Glossy {Albedo = ballWhite, Smoothness = percent}, Radius) {Position = position.WYX});
			}

			Camera camera = new Camera(42f) {Position = new Float3(0f, Half, -Half)};

			float radian = camera.FieldOfView / 2f * Scalars.DegreeToRadian;
			camera.Position += Float3.backward * (Half / MathF.Tan(radian));

			children.Add(camera);
		}
	}
}