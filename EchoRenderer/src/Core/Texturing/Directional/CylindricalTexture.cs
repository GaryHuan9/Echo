using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Rendering.Distributions;

namespace EchoRenderer.Core.Texturing.Directional;

public class CylindricalTexture : IDirectionalTexture
{
	NotNull<Texture> _texture = Texture.black;

	public Texture Texture
	{
		get => _texture;
		set => _texture = value;
	}

	Piecewise2 piecewise;

	/// <summary>
	/// The Jacobian used to convert from uv coordinates to spherical coordinates.
	/// NOTE: we are also missing the one over sin phi term.
	/// </summary>
	const float Jacobian = 1f / Scalars.TAU / Scalars.PI;

	public Vector128<float> Average { get; private set; }

	public void Prepare()
	{
		Texture texture = Texture;
		Int2 size = texture.ImportanceSamplingResolution;

		Assert.IsTrue(size > Int2.zero);
		Float2 sizeR = 1f / size;

		//Fetch temporary buffers to calculate weights
		using var _0 = SpanPool<float>.Fetch(size.Product, out Span<float> weights);
		using var _1 = SpanPool<float>.Fetch(size.x + 1, out Span<float> pastWeights);

		//Get the first luminance weights of discrete points when v = 0
		for (int x = 0; x <= size.x; x++) pastWeights[x] = GetWeight(new Float2(x, 0f));

		//Fill actual weights by summing the four weights of each pixel corner
		int index = -1;

		//At the same time, also calculate the weighted sum of the texture
		Vector256<double> sum = Vector256<double>.Zero;
		double sinTotal = 0d; //Total of the sin multiplier

		for (int y = 1; y <= size.y; y++)
		{
			float sin = MathF.Sin(Scalars.PI * (y - 0.5f) * sizeR.y);
			float previous = GetWeight(new Float2(0f, y));

			sinTotal = Math.FusedMultiplyAdd(sin, size.x, sinTotal);

			for (int x = 1; x <= size.x; x++)
			{
				var position = new Float2(x, y);

				//Calculate and assign weight
				ref float bottomLeft = ref pastWeights[x - 1];
				float current = GetWeight(position);
				float weight = current + previous + bottomLeft + pastWeights[x];

				weights[++index] = weight * sin;

				bottomLeft = previous;
				previous = current;

				//Add to sum with sin weight
				sum = FMA(Get(position - Float2.half), sin, sum);
			}
		}

		//Construct piecewise distribution and calculate average from sum
		piecewise = new Piecewise2(weights, size.x);
		Average = Divide(sum, sinTotal);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		Vector128<float> Get(in Float2 position) => texture[position * sizeR];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		float GetWeight(in Float2 position) => PackedMath.GetLuminance(Get(position));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static Vector256<double> FMA(in Vector128<float> value, float multiplier, in Vector256<double> adder)
		{
			if (Fma.IsSupported && Avx.IsSupported)
			{
				Vector256<double> converted = Avx.ConvertToVector256Double(value);
				Vector256<double> multiplierV = Vector256.Create((double)multiplier);

				return Fma.MultiplyAdd(converted, multiplierV, adder);
			}

			return Vector256.Create
			(
				value.GetElement(0) * multiplier + adder.GetElement(0), value.GetElement(1) * multiplier + adder.GetElement(1),
				value.GetElement(2) * multiplier + adder.GetElement(2), value.GetElement(3) * multiplier + adder.GetElement(3)
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static Vector128<float> Divide(in Vector256<double> value, double divisor)
		{
			if (Avx.IsSupported) return Avx.ConvertToVector128Single(Avx.Divide(value, Vector256.Create(divisor)));

			return Vector128.Create
			(
				(float)(value.GetElement(0) / divisor), (float)(value.GetElement(1) / divisor),
				(float)(value.GetElement(2) / divisor), (float)(value.GetElement(3) / divisor)
			);
		}
	}

	/// <inheritdoc/>
	public Vector128<float> Evaluate(in Float3 direction) => Texture[ToUV(direction)];

	/// <inheritdoc/>
	public Vector128<float> Sample(Distro2 distro, out Float3 incident, out float pdf)
	{
		Float2 uv = piecewise.SampleContinuous(distro, out pdf);

		if (!FastMath.Positive(pdf))
		{
			incident = default;
			return Vector128<float>.Zero;
		}

		float angle0 = uv.x * Scalars.TAU;
		float angle1 = uv.y * Scalars.PI;

		FastMath.SinCos(angle0, out float sinT, out float cosT); //Theta
		FastMath.SinCos(angle1, out float sinP, out float cosP); //Phi

		incident = new Float3(-sinP * sinT, -cosP, -sinP * cosT);

		if (sinP <= 0f) pdf = 0f;
		else pdf *= Jacobian / sinP;

		return Texture[uv];
	}

	/// <inheritdoc/>
	public float ProbabilityDensity(in Float3 incident)
	{
		Float2 uv = ToUV(incident);
		float cosP = -incident.y;
		float sinP = FastMath.Identity(cosP);

		if (sinP <= 0f) return 0f;

		return piecewise.ProbabilityDensity((Distro2)uv) * Jacobian / sinP;
	}

	static Float2 ToUV(in Float3 direction) => new
	(
		FastMath.FMA(MathF.Atan2(direction.x, direction.z), 0.5f / Scalars.PI, 0.5f),
		FastMath.FMA(MathF.Acos(FastMath.Clamp11(direction.y)), -1f / Scalars.PI, 1f)
	);
}