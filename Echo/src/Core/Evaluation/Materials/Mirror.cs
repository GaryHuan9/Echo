using Echo.Common.Memory;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

public class Mirror : Material
{
	public override void Scatter(ref Touch touch, Allocator allocator)
	{
		var make = new MakeBSDF(ref touch, allocator);

		var albedo = (RGB128)SampleAlbedo(touch);
		if (albedo.IsZero) return;

		make.Add<SpecularReflection>().Reset(albedo);
	}
}