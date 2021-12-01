using System;
using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.Materials
{
	public class Glass : MaterialOld
	{
		public float IndexOfRefraction { get; set; } = 1f;
		public float Roughness { get; set; }

		public Texture IndexOfRefractionMap { get; set; } = Texture.white;
		public Texture RoughnessMap { get; set; } = Texture.white;

		float randomRadius;

		public override void Press()
		{
			base.Press();

			AssertZeroOne(Albedo);
			AssertNonNegative(IndexOfRefraction);
			AssertZeroOne(Roughness);

			randomRadius = RoughnessToRandomRadius(Roughness);
		}

		//Equations based from: https://www.scratchapixel.com/lessons/3d-basic-rendering/introduction-to-shading/reflection-refraction-fresnel

		public override Float3 BidirectionalScatter(in TraceQuery query, ExtendedRandom random, out Float3 direction)
		{
			ref readonly Float3 inDirection = ref query.ray.direction;
			ref readonly Float2 texcoord = ref query.shading.texcoord;

			if (AlphaTest(query, out Float3 color))
			{
				direction = inDirection;
				return Float3.one;
			}

			//Refraction and reflection
			Float3 hitNormal = query.shading.normal;
			bool backface = inDirection.Dot(hitNormal) > 0f;

			float etaI = 1f;
			float etaT = SampleTexture(IndexOfRefractionMap, IndexOfRefraction, texcoord);

			if (backface) //If hit back face
			{
				CodeHelper.Swap(ref etaI, ref etaT);
				hitNormal = -hitNormal;
			}

			float radius = SampleTexture(RoughnessMap, randomRadius, texcoord);
			Float3 faceNormal = (hitNormal + random.NextInSphere(radius)).Normalized;

			float cosI = inDirection.Dot(faceNormal);
			if (cosI < 0f) cosI = -cosI;

			float eta = etaI / etaT;
			float cosT2 = 1f - eta * eta * (1f - cosI * cosI);

			float reflectChance;
			float cosT = default;

			if (cosT2 < 0f) reflectChance = 1f; //Total internal reflection, not possible for refraction
			else
			{
				cosT = MathF.Sqrt(cosT2);

				//Fresnel equation
				float ti = etaT * cosI;
				float it = etaI * cosT;

				float ii = etaI * cosI;
				float tt = etaT * cosT;

				float Rs = (ti - it) / (ti + it);
				float Rp = (ii - tt) / (ii + tt);

				reflectChance = (Rs * Rs + Rp * Rp) / 2f;
			}

			//Randomly select between reflection or refraction
			if (random.NextFloat() < reflectChance) direction = inDirection.Reflect(faceNormal); //Reflection
			else direction = (eta * inDirection + (eta * cosI - cosT) * faceNormal).Normalized;  //Refraction

			return color;
		}
	}
}