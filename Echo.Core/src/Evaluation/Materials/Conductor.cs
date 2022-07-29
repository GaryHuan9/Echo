using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

public sealed class Conductor : Material
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

		RGB128 index = Sample(RefractiveIndex, contact);
		RGB128 roughness = Sample(Roughness, contact);

		// float alphaX = IMicrofacet.GetAlpha(FastMath.Clamp01(roughness.R));
		// float alphaY = IMicrofacet.GetAlpha(FastMath.Clamp01(roughness.G));

		float alphaX = roughness.R;
		float alphaY = roughness.G;

		bsdf.Add<GlossyReflection<TrowbridgeReitzMicrofacet, ConductorFresnel>>(allocator).Reset
		(
			new TrowbridgeReitzMicrofacet(alphaX, alphaY),
			new ConductorFresnel(RGB128.White, index, albedo)
		);

		return bsdf;
	}
}