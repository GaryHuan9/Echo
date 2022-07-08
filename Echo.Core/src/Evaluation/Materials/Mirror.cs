using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

public class Mirror : Material
{
	public override void Scatter(ref Contact contact, Allocator allocator)
	{
		var make = new MakeBSDF(ref contact, allocator);

		var albedo = (RGB128)SampleAlbedo(contact);
		if (albedo.IsZero) return;

		make.Add<SpecularReflection>().Reset(albedo);
	}
}