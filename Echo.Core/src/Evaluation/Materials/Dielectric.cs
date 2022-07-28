using System;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

public class Dielectric : Material
{
	NotNull<Texture> _roughness = Pure.black;
	NotNull<Texture> _refractiveIndex = Pure.white;

	public Texture Roughness
	{
		get => _roughness;
		set => _roughness = value;
	}

	public Texture RefractiveIndex
	{
		get => _refractiveIndex;
		set => _refractiveIndex = value;
	}

	public override void Scatter(ref Contact contact, Allocator allocator)
	{
		var make = new MakeBSDF(ref contact, allocator);

		var albedo = (RGB128)SampleAlbedo(contact);
		if (albedo.IsZero) return;

		float index = Sample(RefractiveIndex, contact).R;
		RGB128 roughness = Sample(Roughness, contact);
		float roughnessU = FastMath.Clamp01(roughness.R);
		float roughnessV = FastMath.Clamp01(roughness.G);

		if (!FastMath.AlmostZero(roughnessU) || !FastMath.AlmostZero(roughnessV))
		{
			throw new NotImplementedException();
		}
		else make.Add<SpecularFresnel>().Reset(albedo, 1f, index);
	}
}