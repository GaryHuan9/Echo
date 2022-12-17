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
		float alphaX = IMicrofacet.GetAlpha(roughness.R, out bool specularX);
		float alphaY = IMicrofacet.GetAlpha(roughness.G, out bool specularY);

		float index = Sample(RefractiveIndex, contact).R;
		var fresnel = new RealFresnel(1f, index);

		if (!specularX || !specularY)
		{
			var microfacet = new PBRTv4TrowbridgeReitzMicrofacet(alphaX, alphaY);

			bsdf.Add<GlossyReflection<PBRTv4TrowbridgeReitzMicrofacet, RealFresnel>>(allocator).Reset
			(
				microfacet,
				fresnel
			);

			bsdf.Add<GlossyTransmission<PBRTv4TrowbridgeReitzMicrofacet>>(allocator).Reset
			(
				microfacet,
				fresnel
			);
		}
		else bsdf.Add<SpecularFresnel>(allocator).Reset(fresnel);

		return bsdf;
	}
}