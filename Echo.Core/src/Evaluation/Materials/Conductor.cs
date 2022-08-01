using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

public sealed class Conductor : Material
{
	NotNull<Texture> _roughness = Pure.black;

	NotNull<Texture> _refractiveIndex = Pure.white;
	NotNull<Texture> _extinction = Pure.white;

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

	public Texture Extinction
	{
		get => _extinction;
		set => _extinction = value;
	}

	public override BSDF Scatter(in Contact contact, Allocator allocator, in RGB128 albedo)
	{
		BSDF bsdf = NewBSDF(contact, allocator, albedo);

		RGB128 roughness = Sample(Roughness, contact);
		float alphaX = IMicrofacet.GetAlpha(FastMath.Clamp01(roughness.R));
		float alphaY = IMicrofacet.GetAlpha(FastMath.Clamp01(roughness.G));

		RGB128 index = Sample(RefractiveIndex, contact);
		RGB128 extinction = Sample(Extinction, contact);

		var fresnel = new ComplexFresnel(RGB128.White, index, extinction);

		if (!FastMath.AlmostZero(alphaX) || !FastMath.AlmostZero(alphaY))
		{
			alphaX = FastMath.Max(alphaX, FastMath.Epsilon);
			alphaY = FastMath.Max(alphaY, FastMath.Epsilon);

			var microfacet = new TrowbridgeReitzMicrofacet(alphaX, alphaY);

			bsdf.Add<GlossyReflection<TrowbridgeReitzMicrofacet, ComplexFresnel>>(allocator).Reset(microfacet, fresnel);
		}
		else bsdf.Add<SpecularReflection<ComplexFresnel>>(allocator).Reset(fresnel);

		return bsdf;
	}
}