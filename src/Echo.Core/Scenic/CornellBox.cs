using System;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Cameras;
using Echo.Core.Scenic.Geometries;
using Echo.Core.Scenic.Hierarchies;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic;

/// <summary>
/// An example <see cref="Scene"/>; identical to `ext/Simple/cornell.echo`.
/// Can be used to study the construction of a <see cref="Scene"/>.
/// </summary>
public class CornellBox : Scene
{
	public CornellBox()
	{
		var green = new Diffuse { Albedo = (Pure)RGBA128.Parse("0x00CB21") };
		var red = new Diffuse { Albedo = (Pure)RGBA128.Parse("0xCB0021") };
		var blue = new Diffuse { Albedo = (Pure)RGBA128.Parse("0x0021CB") };
		var white = new Diffuse { Albedo = (Pure)RGBA128.Parse("0xEEEEF2") };

		var cullable = new OneSided { Base = white };
		var light = new Emissive { Albedo = (Pure)RGBA128.Parse("0xFFFAF4") };

		var glass0 = new Dielectric { Albedo = (Pure)RGBA128.Parse("0xF"), RefractiveIndex = 1.2f };
		var glass1 = new Dielectric { Albedo = (Pure)RGBA128.Parse("0xF"), RefractiveIndex = 1.7f };

		Float2 wallSize = new Float2(10f, 10f);
		Float2 wallSize5 = new Float2(5f, 5f);
		Float3 boxSize0 = new Float3(3f, 3f, 3f);
		Float3 boxSize1 = new Float3(3f, 6f, 3f);

		Add(new PlaneEntity { Material = white, Size = wallSize });                                                                            //Floor
		Add(new PlaneEntity { Material = white, Size = wallSize, Position = new Float3(0f, 10f, 0f), Rotation = new Versor(180f, 0f, 0f) });   //Roof
		Add(new PlaneEntity { Material = blue, Size = wallSize, Position = new Float3(0f, 5f, 5f), Rotation = new Versor(-90f, 0f, 0f) });     //Back
		Add(new PlaneEntity { Material = cullable, Size = wallSize, Position = new Float3(0f, 5f, -5f), Rotation = new Versor(90f, 0f, 0f) }); //Front

		Add(new PlaneEntity { Material = green, Size = wallSize, Position = new Float3(5f, 5f, 0f), Rotation = new Versor(0f, 0f, 90f) });      //Right
		Add(new PlaneEntity { Material = red, Size = wallSize, Position = new Float3(-5f, 5f, 0f), Rotation = new Versor(0f, 0f, -90f) });      //Left
		Add(new PlaneEntity { Material = light, Size = wallSize5, Position = new Float3(0f, 9.99f, 0f), Rotation = new Versor(180f, 0f, 0f) }); //Light

		//Place two boxes in the scene
		Add(new BoxEntity { Material = white, Size = boxSize0, Position = new Float3(2f, 1.5f, -2f), Rotation = new Versor(0f, 21f, 0f) });
		Add(new BoxEntity { Material = white, Size = boxSize1, Position = new Float3(-2f, 3f, 2f), Rotation = new Versor(0f, -21f, 0f) });

		//Or alternatively, place two glass sphere in the scene
		// Add(new SphereEntity { Material = glass0, Radius = 2f, Position = new Float3(2f, 2f, -2f) });
		// Add(new SphereEntity { Material = glass1, Radius = 2f, Position = new Float3(-2f, 2f, 2f) });

		//Camera position adjusted to fit entire FOV into the box
		var camera = new PerspectiveCamera() { Position = new Float3(0f, 5f, -5f) };
		float radian = Scalars.ToRadians(camera.FieldOfView / 2f);
		camera.Position += Float3.Backward * (5f / MathF.Tan(radian));
		Add(camera);
	}
}