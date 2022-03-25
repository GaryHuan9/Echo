using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Scattering;

namespace EchoRenderer.Core.Rendering.Materials;

public class Mirror : Material
{
	public override void Scatter(ref Touch touch, Allocator allocator)
	{
		var make = new MakeBSDF(ref touch, allocator);

		Float3 albedo = Sample(Albedo, touch).XYZ;
		if (!albedo.PositiveRadiance()) return;

		make.Add<SpecularReflection>().Reset(albedo);
	}
}