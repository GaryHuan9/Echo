using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

/// <summary>
/// Models surfaces using a diffuse base coated with specular dielectric properties such as plastic and ceramic. 
/// </summary>
[EchoSourceUsable]
public class CoatedDiffuse : Material
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

	float reflectance; //Cached fresnel diffuse reflectance for when the eta above is one.

	public override void Prepare()
	{
		base.Prepare();
		reflectance = CoatedLambertianReflection.FresnelDiffuseReflectance(1f / RefractiveIndex);
	}

	protected override BSDF Scatter(in Contact contact, Allocator allocator, RGB128 albedo)
	{
		BSDF bsdf = NewBSDF(contact, allocator, albedo);
		var fresnel = new RealFresnel(1f, RefractiveIndex);

		RGB128 roughness = Sample(Roughness, contact);
		float alphaX = IMicrofacet.GetAlpha(roughness.R, out _);
		float alphaY = IMicrofacet.GetAlpha(roughness.G, out _);

		bsdf.Add<CoatedLambertianReflection>(allocator).Reset(albedo, fresnel, reflectance);

		//NOTE: we use a glossy BxDF regardless of whether there is any roughness because the current system seems to have some problem
		//when we mix specular functions with glossy ones and I cannot figure out why (at least with the time I have on hand right now)

		var microfacet = new TrowbridgeReitzMicrofacet(alphaX, alphaY);
		bsdf.Add<GlossyReflection<TrowbridgeReitzMicrofacet, RealFresnel>>(allocator).Reset(microfacet, fresnel);

		return bsdf;
	}
}