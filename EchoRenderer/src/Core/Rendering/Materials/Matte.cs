using CodeHelpers;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Scattering;
using EchoRenderer.Core.Texturing;

namespace EchoRenderer.Core.Rendering.Materials;

public class Matte : Material
{
	NotNull<Texture> _roughness = Texture.black;

	public Texture Roughness
	{
		get => _roughness;
		set => _roughness = value;
	}

	public override void Scatter(ref Touch touch, Allocator allocator)
	{
		var make = new MakeBSDF(ref touch, allocator);

		Float3 albedo = Sample(Albedo, touch).XYZ;
		if (!albedo.PositiveRadiance()) return;

		float roughness = FastMath.Clamp01(Sample(Roughness, touch).X);

		if (FastMath.AlmostZero(roughness)) make.Add<LambertianReflection>().Reset(albedo);
		else make.Add<OrenNayar>().Reset(albedo, roughness * 90f * Scalars.DegreeToRadian);
	}
}