using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
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

	float reflectance; //Cached entrance reflectance for Pure RefractiveIndex textures.

	public override void Prepare()
	{
		base.Prepare();
		reflectance = SpecularLambertian.FresnelDiffuseReflectance(RefractiveIndex);
	}

	protected override BSDF Scatter(in Contact contact, Allocator allocator, in RGB128 albedo)
	{
		BSDF bsdf = NewBSDF(contact, allocator, albedo);
		var fresnel = new RealFresnel(1f, RefractiveIndex);

		var function = bsdf.Add<SpecularLambertian>(allocator);
		function.Reset(albedo, fresnel, reflectance);
		return bsdf;
	}
}