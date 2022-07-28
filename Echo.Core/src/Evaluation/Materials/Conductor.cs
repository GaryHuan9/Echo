using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

public class Conductor : Material
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

	public override void Scatter(ref Contact contact, Allocator allocator)
	{
		var make = new MakeBSDF(ref contact, allocator);

		var albedo = (RGB128)SampleAlbedo(contact);
		if (albedo.IsZero) return;

		RGB128 index = Sample(RefractiveIndex, contact);
		RGB128 roughness = Sample(Roughness, contact);

		// float alphaX = IMicrofacet.GetAlpha(FastMath.Clamp01(roughness.R));
		// float alphaY = IMicrofacet.GetAlpha(FastMath.Clamp01(roughness.G));

		float alphaX = roughness.R;
		float alphaY = roughness.G;

		make.Add<GlossyReflection<TrowbridgeReitzMicrofacet, ConductorFresnel>>().Reset
		(
			RGB128.White,
			new TrowbridgeReitzMicrofacet(alphaX, alphaY),
			new ConductorFresnel(RGB128.White, index, albedo)
		);
	}
}