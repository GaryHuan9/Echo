using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Scattering;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.Materials;

public class Matte : Material
{
	NotNull<Texture> _roughness = Texture.black;

	public Texture Roughness
	{
		get => _roughness;
		set => _roughness = value;
	}

	public override void Scatter(ref Interaction interaction, Arena arena)
	{
		var make = new MakeBSDF(ref interaction, arena);

		Float3 albedo = Sample(Albedo, interaction).XYZ;
		if (!albedo.PositiveRadiance()) return;

		float roughness = FastMath.Clamp01(Sample(Roughness, interaction).x);

		if (FastMath.AlmostZero(roughness)) make.Add<LambertianReflection>().Reset(albedo);
		else make.Add<OrenNayar>().Reset(albedo, roughness * 90f * Scalars.DegreeToRadian);
	}
}