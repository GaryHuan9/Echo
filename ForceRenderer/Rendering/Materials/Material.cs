using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
using ForceRenderer.Mathematics.Intersections;
using ForceRenderer.Rendering.Pixels;
using ForceRenderer.Textures;

namespace ForceRenderer.Rendering.Materials
{
	public abstract class Material
	{
		public Float3 Albedo { get; set; }
		public Float3 Emission { get; set; }

		public Texture AlbedoMap { get; set; } = Texture.white;
		public Texture EmissionMap { get; set; } = Texture.white;

		public float NormalIntensity { get; set; } = 1f;
		public Texture NormalMap { get; set; } = Texture.normal;

		Float4 albedoColor;

		static readonly Vector128<float> normalMultiplier = Vector128.Create(2f);
		static readonly Vector128<float> normalAdder = Vector128.Create(-1f, -1f, -2f, 0f);

		/// <summary>
		/// This method is invoked before render begins during the preparation phase.
		/// Materials can use this method to precalculate any value to be used during render.
		/// </summary>
		public virtual void Press()
		{
			AssertZeroOne(Albedo);

			NormalIntensity = NormalIntensity.Clamp(-1f, 1f);
			albedoColor = Utilities.ToColor(Albedo);
		}

		/// <summary>
		/// Returns the emission of this material.
		/// </summary>
		public virtual Float3 Emit(in CalculatedHit hit, ExtendedRandom random) => SampleTexture(EmissionMap, Emission, hit.texcoord);

		/// <summary>
		/// Returns the bidirectional scattering distribution function value of
		/// this material and outputs the randomly scattered direction.
		/// </summary>
		public abstract Float3 BidirectionalScatter(in CalculatedHit hit, ExtendedRandom random, out Float3 direction);

		public unsafe void ApplyNormal(in CalculatedHit hit)
		{
			if (NormalMap == Texture.normal || Scalars.AlmostEquals(NormalIntensity, 0f)) return;

			Vector128<float> sample = NormalMap.GetPixel(hit.texcoord);
			Vector128<float> local = Fma.MultiplyAdd(sample, normalMultiplier, normalAdder);

			//Transform local direction to world space based on normal
			Float3 normal = hit.normal;
			Float3 helper = Math.Abs(normal.x) >= 0.9f ? Float3.forward : Float3.right;

			Float3 tangent = Float3.Cross(normal, helper).Normalized;
			Float3 binormal = Float3.Cross(normal, tangent).Normalized;

			float* p = (float*)&local;

			//Transforms direction using 3x3 matrix multiplication
			normal -= new Float3
					  (
						  p[0] * tangent.x + p[1] * binormal.x + p[2] * normal.x,
						  p[0] * tangent.y + p[1] * binormal.y + p[2] * normal.y,
						  p[0] * tangent.z + p[1] * binormal.z + p[2] * normal.z
					  ) * NormalIntensity;

			fixed (Float3* pointer = &hit.normal) *pointer = normal;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected bool AlphaTest(in CalculatedHit hit, out Float3 color, out Float3 direction)
		{
			Float4 sample = SampleTexture(AlbedoMap, albedoColor, hit.texcoord);
			color = sample.XYZ;

			if (Scalars.AlmostEquals(sample.w, 0f))
			{
				direction = hit.direction;
				return true;
			}

			direction = Float3.zero;
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static float SmoothnessToRandomRadius(float smoothness) => RoughnessToRandomRadius(1f - smoothness);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static float RoughnessToRandomRadius(float roughness)
		{
			const float Alpha = 7.4f;
			const float Beta = 1.8f;

			float radius = MathF.Pow(Alpha, roughness) - 1f;
			return MathF.Pow(radius / (Alpha - 1f), Beta);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static float SampleTexture(Texture texture, float value, Float2 texcoord)
		{
			if (texture == Texture.white) return value;
			if (texture == Texture.black) return 0f;

			return Scalars.AlmostEquals(value, 0f) ? 0f : value * texture[texcoord].x;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static Float2 SampleTexture(Texture texture, Float2 value, Float2 texcoord)
		{
			if (texture == Texture.white) return value;
			if (texture == Texture.black) return Float2.zero;

			return value == Float2.zero ? Float2.zero : value * texture[texcoord].XY;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static Float3 SampleTexture(Texture texture, in Float3 value, Float2 texcoord)
		{
			if (texture == Texture.white) return value;
			if (texture == Texture.black) return Float3.zero;

			return value == Float3.zero ? Float3.zero : value * texture[texcoord].w;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static Float4 SampleTexture(Texture texture, in Float4 value, Float2 texcoord)
		{
			if (texture == Texture.white) return value;
			if (texture == Texture.black) return Float4.zero;

			return value == Float4.zero ? Float4.zero : value * texture[texcoord];
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