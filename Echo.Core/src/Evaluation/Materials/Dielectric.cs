using System;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

public sealed class Dielectric : Material
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

	public override BSDF Scatter(in Contact contact, Allocator allocator, in RGB128 albedo)
	{
		BSDF bsdf = NewBSDF(contact, allocator, albedo);

		RGB128 roughness = Sample(Roughness, contact);
		float roughnessU = FastMath.Clamp01(roughness.R);
		float roughnessV = FastMath.Clamp01(roughness.G);

		float index = Sample(RefractiveIndex, contact).R;

		if (!FastMath.AlmostZero(roughnessU) || !FastMath.AlmostZero(roughnessV))
		{
			throw new NotImplementedException();
		}
		else bsdf.Add<SpecularFresnel>(allocator).Reset(1f, index);

		return bsdf;
	}
}