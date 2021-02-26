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
		public float NormalIntensity { get; set; } = 1f;

		public Texture NormalMap { get; set; } = Texture.normal;

		/// <summary>
		/// This method is invoked before render begins during the preparation phase.
		/// Materials can use this method to precalculate any value to be used during render.
		/// </summary>
		public virtual void Press()
		{
			NormalIntensity = NormalIntensity.Clamp(-1f, 1f);
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

		public unsafe void ApplyNormal(in CalculatedHit hit)
		{
			if (NormalMap == Texture.normal || Scalars.AlmostEquals(NormalIntensity, 0f)) return;

			Float3 sample = NormalMap[hit.texcoord].XYZ;

			float x = sample.x * 2f - 1f;
			float y = sample.y * 2f - 1f;
			float z = sample.z * 2f - 2f;

			//Transform local direction to world space based on normal
			Float3 normal = hit.normal;
			Float3 helper = Math.Abs(normal.x) >= 0.9f ? Float3.forward : Float3.right;

			Float3 tangent = Float3.Cross(normal, helper).Normalized;
			Float3 binormal = Float3.Cross(normal, tangent).Normalized;

			//Transforms direction using 3x3 matrix multiplication
			normal -= new Float3
					  (
						  x * tangent.x + y * binormal.x + z * normal.x,
						  x * tangent.y + y * binormal.y + z * normal.y,
						  x * tangent.z + y * binormal.z + z * normal.z
					  ) * NormalIntensity;

			fixed (Float3* pointer = &hit.normal) *pointer = normal;
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
			if (texture == Texture.white) return value;
			if (texture == Texture.black) return 0f;

			return value * texture[texcoord].x;
		}

		protected static Float2 SampleTexture(Texture texture, Float2 value, Float2 texcoord)
		{
			if (texture == Texture.white) return value;
			if (texture == Texture.black) return Float2.zero;

			return value * texture[texcoord].XY;
		}

		protected static Float3 SampleTexture(Texture texture, in Float3 value, Float2 texcoord)
		{
			if (texture == Texture.white) return value;
			if (texture == Texture.black) return Float3.zero;

			return value * texture[texcoord].XYZ;
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