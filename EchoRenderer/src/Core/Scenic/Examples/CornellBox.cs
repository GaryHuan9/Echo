using System;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common;
using EchoRenderer.Common.Coloring;
using EchoRenderer.Core.Rendering.Materials;
using EchoRenderer.Core.Scenic.Geometries;
using EchoRenderer.Core.Texturing;

namespace EchoRenderer.Core.Scenic.Examples
{
	public class CornellBox : Scene
	{
		public CornellBox()
		{
			var green = new Matte { Albedo = (Pure)RGBA128.Parse("0x00CB21") };
			var red = new Matte { Albedo = (Pure)RGBA128.Parse("0xCB0021") };
			var blue = new Matte { Albedo = (Pure)RGBA128.Parse("0x0021CB") };
			var white = new Matte { Albedo = (Pure)RGBA128.Parse("0xEEEEF2") };

			var cullable = new Cullable { Base = white };
			var light = new Emissive { Albedo = (Pure)RGBA128.Parse("0xFFFAF4") };

			const float Width = 10f;
			const float Half = Width / 2f;
			const float Size = Half / 5f * 3f;

			children.Add(new PlaneEntity { Material = white, Size = (Float2)Width, Position = Float3.Zero, Rotation = Float3.Zero });                                //Floor
			children.Add(new PlaneEntity { Material = white, Size = (Float2)Width, Position = new Float3(0f, Width, 0f), Rotation = new Float3(180f, 0f, 0f) });     //Roof
			children.Add(new PlaneEntity { Material = blue, Size = (Float2)Width, Position = new Float3(0f, Half, Half), Rotation = new Float3(-90f, 0f, 0f) });     //Back
			children.Add(new PlaneEntity { Material = cullable, Size = (Float2)Width, Position = new Float3(0f, Half, -Half), Rotation = new Float3(90f, 0f, 0f) }); //Front

			children.Add(new PlaneEntity { Material = green, Size = (Float2)Width, Position = new Float3(Half, Half, 0f), Rotation = new Float3(0f, 0f, 90f) }); //Right
			children.Add(new PlaneEntity { Material = red, Size = (Float2)Width, Position = new Float3(-Half, Half, 0f), Rotation = new Float3(0f, 0f, -90f) }); //Left
			children.Add(new PlaneEntity { Material = light, Size = (Float2)Half, Position = new Float3(0f, Width - 0.01f, 0f), Rotation = Float3.Zero });       //Light

			children.Add(new BoxEntity { Material = white, Size = (Float3)Size, Position = new Float3(Size / 1.5f, Size / 2f, -Size / 1.5f), Rotation = Float3.Up * 21f });
			children.Add(new BoxEntity { Material = white, Size = new Float3(Size, Size * 2f, Size), Position = new Float3(-Size / 1.5f, Size, Size / 1.5f), Rotation = Float3.Up * -21f });

			// const float Radius = 0.4f;
			// const int Count = 3;
			//
			// Float3 ballWhite = Utilities.ToColor("F7F7FD").XYZ;
			//
			// for (int i = -Count; i <= Count; i++)
			// {
			// 	float percent = Scalars.InverseLerp(-Count, Count, (float)i);
			// 	Float4 position = new Float4(i * Radius * 2.1f, Width - Radius, Radius - Half, Half - Radius);
			//
			// 	children.Add(new SphereObject(new Glass { Albedo = ballWhite, IndexOfRefraction = 1.5f, Roughness = percent }, Radius) { Position = position.ZYX });
			// 	children.Add(new SphereObject(new Glass { Albedo = ballWhite, IndexOfRefraction = Scalars.Lerp(1.1f, 2.1f, percent) }, Radius) { Position = position.XYW });
			// 	children.Add(new SphereObject(new Glossy { Albedo = ballWhite, Smoothness = percent }, Radius) { Position = position.WYX });
			// }

			Camera camera = new Camera(42f) { Position = new Float3(0f, Half, -Half) };

			float radian = Scalars.ToRadians(camera.FieldOfView / 2f);
			camera.Position += Float3.Backward * (Half / MathF.Tan(radian));

			children.Add(camera); //Position: (0, 5, -18.025444)
		}
	}
}