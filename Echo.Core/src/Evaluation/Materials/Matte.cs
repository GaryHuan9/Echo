﻿using CodeHelpers;
using CodeHelpers.Mathematics;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

public class Matte : Material
{
	NotNull<Texture> _roughness = Texture.black;

	public Texture Roughness
	{
		get => _roughness;
		set => _roughness = value;
	}

	public override void Scatter(ref Touch touch, Allocator allocator)
	{
		var make = new MakeBSDF(ref touch, allocator);

		var albedo = (RGB128)SampleAlbedo(touch);
		if (albedo.IsZero) return;

		float roughness = FastMath.Clamp01(Sample(Roughness, touch).Luminance); //OPTIMIZE get primary channel

		if (FastMath.AlmostZero(roughness)) make.Add<LambertianReflection>().Reset(albedo);
		else make.Add<OrenNayar>().Reset(albedo, Scalars.ToRadians(roughness * 90f));
	}
}