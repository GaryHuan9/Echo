using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Evaluation.Scattering;
using EchoRenderer.Core.Textures.Colors;

namespace EchoRenderer.Core.Evaluation.Materials;

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