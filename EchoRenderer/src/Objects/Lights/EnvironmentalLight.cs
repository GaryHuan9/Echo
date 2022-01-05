using System;
using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Objects.Preparation;
using EchoRenderer.Rendering.Distributions;
using EchoRenderer.Textures.Directional;

namespace EchoRenderer.Objects.Lights
{
	public class EnvironmentalLight : LightSource
	{
		public EnvironmentalLight() : base(LightType.area | LightType.infinite) { }

		NotNull<object> _texture = Textures.Texture.black; //Interfaces and implicit casts are not so nice to each other so object is used here

		public IDirectionalTexture Texture
		{
			get => (IDirectionalTexture)_texture.Value;
			set => _texture = new NotNull<object>(value);
		}

		public override void Prepare(PreparedScene scene)
		{
			base.Prepare(scene);
			Texture.Prepare();
		}

		public override Float3 Sample(in Interaction interaction, Distro2 distro, out Float3 incidentWorld, out float pdf, out float travel)
		{
			throw new NotImplementedException();
		}

		public override float ProbabilityDensity(in Interaction interaction, in Float3 incidentWorld)
		{
			throw new NotImplementedException();
		}
	}
}