using System;
using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Scattering;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.Materials
{
	public class Matte : MaterialNew
	{
		public Texture Deviation { get; set; }

		public override void BeforeRender()
		{
			base.BeforeRender();

			Deviation ??= Texture.black;
		}

		public override void Scatter(ref HitQuery query, Arena arena)
		{
			FillTangentNormal(ref query);

			ref BSDF bsdf = ref query.bsdf;
			bsdf = arena.allocator.New<BSDF>();
			bsdf.Reset(query);

			Float3 albedo = Utilities.ToFloat3(Albedo[query.uv]);
			if (arena.profile.IsZero(albedo)) return;

			float deviation = FastMath.Clamp01(Deviation[query.uv].GetElement(0));

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
