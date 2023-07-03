using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

/// <summary>
/// Models dielectric surfaces such as glass which both reflect and refract light.
/// </summary>
[EchoSourceUsable]
public sealed class Dielectric : Material
{
	[EchoSourceUsable]
	public float RefractiveIndex { get; set; } = 1.5f;

	NotNull<Texture> _roughness = Pure.black;

	[EchoSourceUsable]
	public Texture Roughness
	{
		get => _roughness;
		set => _roughness = value;
	}

	protected override BSDF Scatter(in Contact contact, Allocator allocator, RGB128 albedo)
	{
		BSDF bsdf = NewBSDF(contact, allocator, albedo);
		var fresnel = new RealFresnel(1f, RefractiveIndex);

		RGB128 roughness = Sample(Roughness, contact);
		float alphaX = IMicrofacet.GetAlpha(roughness.R, out bool specularX);
		float alphaY = IMicrofacet.GetAlpha(roughness.G, out bool specularY);

		if (!specularX || !specularY)
		{
			var microfacet = new TrowbridgeReitzMicrofacet(alphaX, alphaY);

			bsdf.Add<GlossyReflection<TrowbridgeReitzMicrofacet, RealFresnel>>(allocator).Reset(microfacet, fresnel);
			bsdf.Add<GlossyTransmission<TrowbridgeReitzMicrofacet>>(allocator).Reset(microfacet, fresnel);
		}
		else bsdf.Add<SpecularFresnel>(allocator).Reset(fresnel);

		return bsdf;
	}
}