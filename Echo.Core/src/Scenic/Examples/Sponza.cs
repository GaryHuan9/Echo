﻿using CodeHelpers.Packed;
using Echo.Core.InOut;
using Echo.Core.Scenic.Geometries;
using Echo.Core.Scenic.Lights;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Examples;

public class Sponza : Scene
{
	public Sponza()
	{
		var mesh = new Mesh("Assets/Models/CrytekSponza/sponza.obj");
		var materials = new MaterialLibrary("Assets/Models/CrytekSponza/sponza.mat");

		// materials["light"] = new Invisible();

		children.Add(new MeshEntity { Mesh = mesh, MaterialLibrary = materials, Rotation = Float3.Up * 90f });

		children.Add(new AmbientLight { Texture = (Pure)new RGBA128(10.3f, 8.9f, 6.3f) });

		children.Add(new Camera(90f) { Position = new Float3(-9.4f, 16.1f, -4.5f), Rotation = new Float3(13.8f, 43.6f, 0f) });
		// children.Add(new Camera(90f) {Position = new Float3(2.8f, 7.5f, -1.7f), Rotation = new Float3(6.8f, -12.6f, 0f)});
	}
}