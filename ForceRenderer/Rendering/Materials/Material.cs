using System;
using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
using ForceRenderer.Textures;

namespace ForceRenderer.Rendering.Materials
{
	public abstract class Material
	{
		/// <summary>
		/// This method is invoked before render begins during the preparation phase.
		/// Materials can use this method to precalculate any value to be used during render.
		/// </summary>
		public virtual void Press() { }

		/// <summary>
		/// Returns the emission of this material.
		/// </summary>
		public abstract Float3 Emit(in CalculatedHit hit, ExtendedRandom random);

		/// <summary>
		/// Returns the bidirectional scattering distribution function value of
		/// this material and outputs the randomly scattered direction.
		/// </summary>
		public abstract Float3 BidirectionalScatter(in CalculatedHit hit, ExtendedRandom random, out Float3 direction);

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