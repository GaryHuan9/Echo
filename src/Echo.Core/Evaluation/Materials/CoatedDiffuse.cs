using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.InOut.EchoDescription;
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

	float reflectance; //Cached fresnel diffuse reflectance for when the eta above is one.

	public override void Prepare()
	{
		base.Prepare();
		reflectance = SpecularLambertian.FresnelDiffuseReflectance(1f / RefractiveIndex);
	}

	protected override BSDF Scatter(in Contact contact, Allocator allocator, in RGB128 albedo)
	{
		BSDF bsdf = NewBSDF(contact, allocator, albedo);
		var fresnel = new RealFresnel(1f, RefractiveIndex);

		bsdf.Add<SpecularLambertian>(allocator).Reset(albedo, fresnel, reflectance);
		return bsdf;
	}
}