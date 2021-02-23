using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
using ForceRenderer.Textures;

namespace ForceRenderer.Rendering.Materials
{
	public abstract class Material
	{
		public Texture NormalMap { get; set; } = Texture2D.normal;
		public float NormalIntensity { get; set; } = 1f;

		Vector128<float> normalMultiplier;

		static readonly Vector128<float> normalDefault = Vector128.Create(0.5f, 0.5f, 1f, 0f);

		/// <summary>
		/// This method is invoked before render begins during the preparation phase.
		/// Materials can use this method to precalculate any value to be used during render.
		/// </summary>
		public virtual void Press()
		{
			normalMultiplier = Vector128.Create(NormalIntensity, NormalIntensity, NormalIntensity, 0f);
		}

		/// <summary>
		/// Returns the emission of this material.
		/// </summary>
		public abstract Float3 Emit(in CalculatedHit hit, ExtendedRandom random);

		/// <summary>
		/// Returns the bidirectional scattering distribution function value of
		/// this material and outputs the randomly scattered direction.
		/// </summary>
		public abstract Float3 BidirectionalScatter(in CalculatedHit hit, ExtendedRandom random, out Float3 direction);

		public void ApplyNormal(ref CalculatedHit hit)
		{
			if (NormalMap == Texture2D.normal || Scalars.AlmostEquals(NormalIntensity, 0f)) return;

			Float3 sample = NormalMap[hit.texcoord];

			unsafe
			{
				Vector128<float> normalVector = Sse.LoadVector128(&sample.x);

				normalVector = Sse.Subtract(normalVector, normalDefault);
				normalVector = Sse.Multiply(normalVector, normalMultiplier);
			}

			// if (sample)
		}

		protected static float SmoothnessToRandomRadius(float smoothness) => RoughnessToRandomRadius(1f - smoothness);

		protected static float RoughnessToRandomRadius(float roughness)
		{
			const float Alpha = 7.4f;
			const float Beta = 1.8f;

			float radius = MathF.Pow(Alpha, roughness) - 1f;
			return MathF.Pow(radius / (Alpha - 1f), Beta);
		}

		protected static float SampleTexture(Texture texture, float value, Float2 texcoord)
		{
			if (texture == Texture2D.white) return value;
			return value * texture[texcoord].x;
		}

		protected static Float2 SampleTexture(Texture texture, Float2 value, Float2 texcoord)
		{
			if (texture == Texture2D.white) return value;
			return value * texture[texcoord].XY;
		}

		protected static Float3 SampleTexture(Texture texture, in Float3 value, Float2 texcoord)
		{
			if (texture == Texture2D.white) return value;
			return value * texture[texcoord];
		}

		protected static void AssertZeroOne(float value)
		{
			if (0f <= value && value <= 1f) return;
			throw new Exception($"Invalid value outside of bounds 0 to 1: {value}");
		}

		protected static void AssertZeroOne(Float2 value)
		{
			if (0f <= value.MinComponent && value.MaxComponent <= 1f) return;
			throw new Exception($"Invalid value outside of bounds 0 to 1: {value}");
		}

		protected static void AssertZeroOne(Float3 value)
		{
			if (0f <= value.MinComponent && value.MaxComponent <= 1f) return;
			throw new Exception($"Invalid value outside of bounds 0 to 1: {value}");
		}

		protected static void AssertNonNegative(float value)
		{
			if (0f <= value) return;
			throw new Exception($"Invalid negative value: {value}");
		}

		protected static void AssertNonNegative(Float2 value)
		{
			if (0f <= value.MinComponent) return;
			throw new Exception($"Invalid negative value: {value}");
		}

		protected static void AssertNonNegative(Float3 value)
		{
			if (0f <= value.MinComponent) return;
			throw new Exception($"Invalid negative value: {value}");
		}
	}
}