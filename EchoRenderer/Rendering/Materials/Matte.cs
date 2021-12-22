using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
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

		public override void Press()
		{
			base.Press();

			Deviation ??= Texture.black;
		}

		public override void Scatter(ref Interaction interaction, Arena arena)
		{
			ref BSDF bsdf = ref interaction.bsdf;
			bsdf = arena.allocator.New<BSDF>();
			bsdf.Reset(interaction);

			Float3 albedo = Sample(Albedo, interaction).XYZ;
			if (arena.profile.IsZero(albedo)) return;

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