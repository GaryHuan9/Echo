using System;
using CodeHelpers;
using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
using ForceRenderer.Textures;

namespace ForceRenderer.Rendering.Materials
{
	public class Glass : Material
	{
		public Float3 Transmission { get; set; }
		public float IndexOfRefraction { get; set; }
		public float Roughness { get; set; }

		public Texture TransmissionMap { get; set; } = Texture2D.white;
		public Texture IndexOfRefractionMap { get; set; } = Texture2D.white;
		public Texture RoughnessMap { get; set; } = Texture2D.white;

		float randomRadius;

		public override void Press()
		{
			base.Press();

			AssertZeroOne(Transmission);
			AssertNonNegative(IndexOfRefraction);
			AssertZeroOne(Roughness);

			randomRadius = RoughnessToRandomRadius(Roughness);
		}

		public override Float3 Emit(in CalculatedHit hit, ExtendedRandom random) => Float3.zero;

		public override Float3 BidirectionalScatter(in CalculatedHit hit, ExtendedRandom random, out Float3 direction)
		{
			Float3 hitNormal = hit.normal;

			float etaI = 1f;
			float etaT = SampleTexture(IndexOfRefractionMap, IndexOfRefraction, hit.texcoord);

			if (hit.direction.Dot(hitNormal) > 0f)
			{
				//Hit backface
				CodeHelper.Swap(ref etaI, ref etaT);
				hitNormal = -hitNormal;
			}

			float radius = SampleTexture(RoughnessMap, randomRadius, hit.texcoord);
			Float3 faceNormal = (hitNormal + random.NextInSphere(radius)).Normalized;

			float cosI = hit.direction.Dot(faceNormal);
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
			if (random.NextFloat() < reflectChance) direction = hit.direction.Reflect(faceNormal); //Reflection
			else direction = (eta * hit.direction + (eta * cosI - cosT) * faceNormal).Normalized;  //Refraction

			return SampleTexture(TransmissionMap, Transmission, hit.texcoord);
		}
	}
}