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

		public override void Scatter(ref HitQuery query, Arena arena)
		{
			FillTangentNormal(ref query);

			ref BSDF bsdf = ref query.bsdf;
			bsdf = arena.allocator.New<BSDF>();
			bsdf.Reset(query);

			Float3 albedo = Utilities.ToFloat3(Albedo[query.uv]);
			if (albedo == Float3.zero) return; //TODO: use luminance comparison

			float sigma = Deviation[query.uv].GetElement(0).Clamp(0f, 90f);

			BxDF function;

			if (sigma.AlmostEquals())
			{
				function = arena.allocator.New<LambertianReflection>();
				((LambertianReflection)function).Reset(albedo);
			}
			else
			{
				function = arena.allocator.New<OrenNayar>();
				((OrenNayar)function).Reset(albedo, sigma);
			}

			bsdf.Add(function);
		}
	}
}