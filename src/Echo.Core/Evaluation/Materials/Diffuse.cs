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
/// Models a surface that scatters light uniformly in general.
/// </summary>
[EchoSourceUsable]
public sealed class Diffuse : Material
{
	/// <summary>
	/// Whether light goes through the material instead of reflecting off of it.
	/// </summary>
	[EchoSourceUsable]
	public bool Transmissive { get; set; } = false;

	NotNull<Texture> _roughness = Pure.black;

	[EchoSourceUsable]
	public Texture Roughness
	{
		get => _roughness;
		set => _roughness = value;
	}

	protected override BSDF Scatter(in Contact contact, Allocator allocator, in RGB128 albedo)
	{
		BSDF bsdf = NewBSDF(contact, allocator, albedo);

		if (!Transmissive)
		{
			float roughness = FastMath.Clamp01(Sample(Roughness, contact).R);

			if (FastMath.AlmostZero(roughness)) bsdf.Add<LambertianReflection>(allocator);
			else bsdf.Add<OrenNayar>(allocator).Reset(roughness);
		}
		else bsdf.Add<Lambertian>(allocator);

		return bsdf;
	}
}