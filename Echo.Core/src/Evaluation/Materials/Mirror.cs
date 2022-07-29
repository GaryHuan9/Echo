using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

public sealed class Mirror : Material
{
	public override BSDF Scatter(in Contact contact, Allocator allocator, in RGB128 albedo)
	{
		BSDF bsdf = NewBSDF(contact, allocator, albedo);
		bsdf.Add<SpecularReflection<PassthroughFresnel>>(allocator).Reset(new PassthroughFresnel());
		return bsdf;
	}
}