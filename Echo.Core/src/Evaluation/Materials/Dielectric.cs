using System;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
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
		float alphaX = IMicrofacet.GetAlpha(roughness.R, out bool isSpecularX);
		float alphaY = IMicrofacet.GetAlpha(roughness.G, out bool isSpecularY);

		float index = Sample(RefractiveIndex, contact).R;

		if (!isSpecularX || !isSpecularY)
		{
			var microfacet = new TrowbridgeReitzMicrofacet(alphaX, alphaY);
			var fresnel = new RealFresnel(1f, index);

			// bsdf.Add<GlossyReflection<TrowbridgeReitzMicrofacet, RealFresnel>>(allocator).Reset
			// (
			// 	microfacet,
			// 	fresnel
			// );

			bsdf.Add<GlossyTransmission<TrowbridgeReitzMicrofacet>>(allocator).Reset
			(
				microfacet,
				1f, index
			);
		}
		else bsdf.Add<SpecularFresnel>(allocator).Reset(1f, index);

		return bsdf;
	}
}