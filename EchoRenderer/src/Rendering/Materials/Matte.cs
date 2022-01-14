using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Scattering;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.Materials
{
	public class Matte : Material
	{
		public Texture Deviation { get; set; }

		public override void Prepare()
		{
			base.Prepare();

			Deviation ??= Texture.black;
		}

		public override void Scatter(ref Interaction interaction, Arena arena)
		{
			BSDF bsdf = interaction.CreateBSDF(arena);

			Float3 albedo = Sample(Albedo, interaction).XYZ;
			if (!albedo.PositiveRadiance()) return;

			float deviation = FastMath.Clamp01(Sample(Deviation, interaction).x);

			BxDF function;

			if (deviation.AlmostEquals())
			{
				function = arena.allocator.New<LambertianReflection>();
				((LambertianReflection)function).Reset(albedo);
			}
			else
			{
				function = arena.allocator.New<OrenNayar>();
				((OrenNayar)function).Reset(albedo, deviation * 90f);
			}

			bsdf.Add(function);
		}
	}
}