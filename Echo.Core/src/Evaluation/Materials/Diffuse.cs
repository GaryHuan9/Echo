using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

public sealed class Diffuse : Material
{
	NotNull<Texture> _roughness = Pure.black;

	public Texture Roughness
	{
		get => _roughness;
		set => _roughness = value;
	}

	public override BSDF Scatter(in Contact contact, Allocator allocator, in RGB128 albedo)
	{
		BSDF bsdf = NewBSDF(contact, allocator, albedo);

		float roughness = FastMath.Clamp01(Sample(Roughness, contact).R);

		if (FastMath.AlmostZero(roughness)) bsdf.Add<LambertianReflection>(allocator);
		else bsdf.Add<OrenNayar>(allocator).Reset(Scalars.ToRadians(roughness * 90f));

		return bsdf;
	}
}