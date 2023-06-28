using System;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Scattering;

/// <summary>
/// Perfectly uniform Lambertian diffuse reflection.
/// </summary>
public class LambertianReflection : BxDF
{
	public LambertianReflection() : this(FunctionType.Reflective) { }

	protected LambertianReflection(FunctionType type) : base(type | FunctionType.Diffuse) { }

	public override RGB128 Evaluate(Float3 outgoing, Float3 incident)
	{
		if (FlatOrOppositeHemisphere(outgoing, incident)) return RGB128.Black;
		return new RGB128(Scalars.PiR);
	}

	public override float ProbabilityDensity(Float3 outgoing, Float3 incident)
	{
		if (FlatOrOppositeHemisphere(outgoing, incident)) return 0f;
		return FastMath.Abs(CosineP(incident)) * Scalars.PiR;
	}

	public override Probable<RGB128> Sample(Sample2D sample, Float3 outgoing, out Float3 incident)
	{
		incident = sample.CosineHemisphere;
		float pdf = CosineP(incident) * Scalars.PiR;

		if (outgoing.Z < 0f) incident = new Float3(incident.X, incident.Y, -incident.Z);
		return (new RGB128(Scalars.PiR), pdf);
	}

	public override RGB128 GetReflectance(Float3 outgoing, ReadOnlySpan<Sample2D> samples) => RGB128.White;
	public override RGB128 GetReflectance(ReadOnlySpan<Sample2D> samples0, ReadOnlySpan<Sample2D> samples1) => RGB128.White;
}

/// <summary>
/// Perfectly uniform Lambertian diffuse transmission.
/// </summary>
public sealed class LambertianTransmission : LambertianReflection
{
	public LambertianTransmission() : base(FunctionType.Transmissive) { }

	public override RGB128 Evaluate(Float3 outgoing, Float3 incident)
	{
		Float3 flipped = new Float3(outgoing.X, outgoing.Y, -outgoing.Z);
		return base.Evaluate(flipped, incident);
	}

	public override float ProbabilityDensity(Float3 outgoing, Float3 incident)
	{
		Float3 flipped = new Float3(outgoing.X, outgoing.Y, -outgoing.Z);
		return base.ProbabilityDensity(flipped, incident);
	}

	public override Probable<RGB128> Sample(Sample2D sample, Float3 outgoing, out Float3 incident)
	{
		Float3 flipped = new Float3(outgoing.X, outgoing.Y, -outgoing.Z);
		return base.Sample(sample, flipped, out incident);
	}
}

/// <summary>
/// Perfectly uniform Lambertian diffuse scattering (both reflection and transmission).
/// </summary>
public sealed class Lambertian : BxDF
{
	public Lambertian() : base(FunctionType.Diffuse | FunctionType.Reflective | FunctionType.Transmissive) { }

	public override RGB128 Evaluate(Float3 outgoing, Float3 incident) => new(Scalars.TauR);

	public override float ProbabilityDensity(Float3 outgoing, Float3 incident) => FastMath.Abs(CosineP(incident)) * Scalars.TauR;

	public override Probable<RGB128> Sample(Sample2D sample, Float3 outgoing, out Float3 incident)
	{
		bool reflect = sample.x > 0.5f;
		sample = new Sample2D(FastMath.Abs(sample.x * 2f - 1f), sample.y);

		incident = sample.CosineHemisphere;
		float pdf = CosineP(incident) * Scalars.TauR;
		bool flip = (outgoing.Z > 0f) ^ reflect;

		if (flip) incident = new Float3(incident.X, incident.Y, -incident.Z);
		return (new RGB128(Scalars.TauR), pdf);
	}
}

/// <summary>
/// Microfacet diffuse reflection model originally proposed by
/// Generalization of Lambert's reflectance model [Oren and Nayar 1994].
/// Implementation based on: https://mimosa-pudica.net/improved-oren-nayar.html
/// </summary>
public sealed class OrenNayar : LambertianReflection
{
	public void Reset(float newRoughness)
	{
		Ensure.IsTrue(newRoughness is >= 0f and <= 1f);

		a = 1f / FastMath.FMA(Scalars.Pi / 2f - 2f / 3f, newRoughness, Scalars.Pi);
		b = a * newRoughness;
	}

	float a;
	float b;

	public override RGB128 Evaluate(Float3 outgoing, Float3 incident)
	{
		if (FlatOrOppositeHemisphere(outgoing, incident)) return RGB128.Black;

		float cosO = FastMath.Abs(CosineP(outgoing));
		float cosI = FastMath.Abs(CosineP(incident));

		float s = outgoing.Dot(incident) - cosO * cosI;
		if (FastMath.Positive(s)) s /= FastMath.Max(cosO, cosI);
		return new RGB128(a + b * s);
	}
}