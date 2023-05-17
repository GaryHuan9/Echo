using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

/// <summary>
/// Models surfaces with both reflective and diffuse properties such as plastic and ceramic. 
/// </summary>
[EchoSourceUsable]
public class Plastic : Material
{
	NotNull<Texture> _roughness = Pure.black;

	[EchoSourceUsable]
	public Texture Roughness
	{
		get => _roughness;
		set => _roughness = value;
	}

	public override BSDF Scatter(in Contact contact, Allocator allocator, in RGB128 albedo)
	{
		BSDF bsdf = NewBSDF(contact, allocator, albedo);

		bsdf.Add<LambertianReflection>(allocator);

		float roughness = FastMath.Clamp01(Sample(Roughness, contact).R);
		float alpha = IMicrofacet.GetAlpha(roughness, out bool specular);
		var fresnel = new RealFresnel(1f, 1.7f);

		if (!specular)
		{
			var microfacet = new TrowbridgeReitzMicrofacet(alpha, alpha);
			bsdf.Add<GlossyReflection<TrowbridgeReitzMicrofacet, RealFresnel>>(allocator).Reset(microfacet, fresnel);
		}
		else bsdf.Add<SpecularReflection<RealFresnel>>(allocator).Reset(fresnel);

		return bsdf;
	}
}