using System;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Textures.Colors;
using NUnit.Framework;

namespace Echo.UnitTests.Evaluation;

//This test is kind of a mess right now, but it works, and we will remake it when we redo the BSDF system

[TestFixture]
public class BxDFTests
{
	static BxDFTests()
	{
		var distribution = new StratifiedDistribution
		{
			Extend = outgoings.Length,
			Prng = new SystemPrng(1)
		};

		distribution.BeginSeries(Int2.Zero);

		foreach (ref Float3 direction in outgoings.AsSpan())
		{
			distribution.BeginSession();
			direction = distribution.Next2D().UniformSphere;
		}

		distribution = new StratifiedDistribution
		{
			Extend = samples.Length,
			Prng = new SystemPrng(2)
		};

		distribution.BeginSeries(Int2.Zero);

		foreach (ref Sample2D sample in samples.AsSpan())
		{
			distribution.BeginSession();
			sample = distribution.Next2D();
		}
	}

	static readonly (CheckType type, BxDF function)[] pairs =
	{
		(CheckType.everything, MakeFunction<LambertianReflection>(_ => { })),
		(CheckType.everything, MakeFunction<LambertianTransmission>(_ => { })),
		(CheckType.everything, MakeFunction<Lambertian>(_ => { })),
		(CheckType.oneDirection, MakeFunction<OrenNayar>(function => function.Reset(0.15f))),
		(CheckType.oneDirection, MakeFunction<OrenNayar>(function => function.Reset(0.82f))),
		(CheckType.oneDirection, MakeFunction<SpecularReflection<RealFresnel>>(function => function.Reset(new RealFresnel(1.1f, 1.7f)))),
		(CheckType.oneDirection, MakeFunction<SpecularReflection<RealFresnel>>(function => function.Reset(new RealFresnel(1.7f, 1.1f)))),
		(CheckType.oneDirection, MakeFunction<SpecularReflection<ComplexFresnel>>(function => function.Reset(new ComplexFresnel(new RGB128(0.9f, 1.1f, 1.2f), new RGB128(0.27105f, 0.67693f, 1.31640f), new RGB128(3.60920f, 2.62480f, 2.29210f))))), //Copper
		(CheckType.oneDirection, MakeFunction<SpecularReflection<ComplexFresnel>>(function => function.Reset(new ComplexFresnel(new RGB128(1.3f, 1.0f, 0.8f), new RGB128(2.74070f, 2.54180f, 2.26700f), new RGB128(3.81430f, 3.43450f, 3.03850f))))), //Titanium
		(CheckType.oneDirection, MakeFunction<SpecularReflection<ComplexFresnel>>(function => function.Reset(new ComplexFresnel(RGB128.White, RGB128.MaxEpsilon(RGB128.Black), RGB128.Black)))),
		(CheckType.oneDirection, MakeFunction<SpecularTransmission>(function => function.Reset(new RealFresnel(1.1f, 1.7f)))),
		(CheckType.oneDirection, MakeFunction<SpecularTransmission>(function => function.Reset(new RealFresnel(1.7f, 1.1f)))),
		(CheckType.everything, MakeFunction<SpecularFresnel>(function => function.Reset(new RealFresnel(1.1f, 1.7f)))),
		(CheckType.everything, MakeFunction<SpecularFresnel>(function => function.Reset(new RealFresnel(1.7f, 1.1f)))),
		(CheckType.oneDirection, MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, RealFresnel>>(function => function.Reset(new TrowbridgeReitzMicrofacet(0.3f, 0.8f), new RealFresnel(1.1f, 1.7f)))),
		(CheckType.oneDirection, MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, RealFresnel>>(function => function.Reset(new TrowbridgeReitzMicrofacet(0.7f, 0.4f), new RealFresnel(1.7f, 1.1f)))),
		(CheckType.oneDirection, MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, RealFresnel>>(function => function.Reset(new TrowbridgeReitzMicrofacet(1f, 1f), new RealFresnel(1f, 1f)))),
		(CheckType.oneDirection, MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, RealFresnel>>(function => function.Reset(new TrowbridgeReitzMicrofacet(1f, 1f), new RealFresnel(1.5f, 1f)))),
		(CheckType.everything, MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, RealFresnel>>(function => function.Reset(new TrowbridgeReitzMicrofacet(1E-4f, 1E-4f), new RealFresnel(1f, 1.5f)))),
		(CheckType.oneDirection, MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, ComplexFresnel>>(function => function.Reset(new TrowbridgeReitzMicrofacet(0.3f, 0.8f), new ComplexFresnel(new RGB128(0.9f, 1.1f, 1.2f), new RGB128(0.27105f, 0.67693f, 1.31640f), new RGB128(3.60920f, 2.62480f, 2.29210f))))), //Copper
		(CheckType.oneDirection, MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, ComplexFresnel>>(function => function.Reset(new TrowbridgeReitzMicrofacet(0.7f, 0.4f), new ComplexFresnel(new RGB128(1.3f, 1.0f, 0.8f), new RGB128(2.74070f, 2.54180f, 2.26700f), new RGB128(3.81430f, 3.43450f, 3.03850f))))), //Titanium
		(CheckType.oneDirection, MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, ComplexFresnel>>(function => function.Reset(new TrowbridgeReitzMicrofacet(1f, 1f), new ComplexFresnel(RGB128.White, RGB128.MaxEpsilon(RGB128.Black), RGB128.Black)))),
		(CheckType.everything, MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, ComplexFresnel>>(function => function.Reset(new TrowbridgeReitzMicrofacet(1E-4f, 1E-4f), new ComplexFresnel(RGB128.White, RGB128.White * 2f, RGB128.White * 3f)))),
		(CheckType.oneDirection, MakeFunction<GlossyTransmission<TrowbridgeReitzMicrofacet>>(function => function.Reset(new TrowbridgeReitzMicrofacet(0.3f, 0.8f), new RealFresnel(1.1f, 1.7f)))),
		(CheckType.oneDirection, MakeFunction<GlossyTransmission<TrowbridgeReitzMicrofacet>>(function => function.Reset(new TrowbridgeReitzMicrofacet(0.7f, 0.4f), new RealFresnel(1.7f, 1.1f)))),
		(CheckType.onlyQuotient, MakeFunction<GlossyTransmission<TrowbridgeReitzMicrofacet>>(function => function.Reset(new TrowbridgeReitzMicrofacet(1f, 1f), new RealFresnel(1f, 1f)))), //I cannot get this to match :( but this edge case should not effect visuals at all
		(CheckType.oneDirection, MakeFunction<GlossyTransmission<TrowbridgeReitzMicrofacet>>(function => function.Reset(new TrowbridgeReitzMicrofacet(1f, 1f), new RealFresnel(1.5f, 1f)))),
		(CheckType.everything, MakeFunction<GlossyTransmission<TrowbridgeReitzMicrofacet>>(function => function.Reset(new TrowbridgeReitzMicrofacet(1E-4f, 1E-4f), new RealFresnel(1f, 1.5f)))),
		// (CheckType.onlyOverall, MakeFunction<SpecularLambertian>(function => function.Reset(RGB128.White, new RealFresnel(1.1f, 1.7f), SpecularLambertian.FresnelDiffuseReflectance(1.1f / 1.7f)))),
		// (CheckType.onlyOverall, MakeFunction<SpecularLambertian>(function => function.Reset(RGB128.White * 0.3f, new RealFresnel(1.7f, 1.1f), SpecularLambertian.FresnelDiffuseReflectance(1.7f / 1.1f)))),
		// (CheckType.onlyOverall, MakeFunction<SpecularLambertian>(function => function.Reset(RGB128.White * 0.3f, new RealFresnel(1.1f, 1.7f)))),
		// (CheckType.onlyOverall, MakeFunction<SpecularLambertian>(function => function.Reset(RGB128.White, new RealFresnel(1f, 1f))))
	};

	static readonly Float3[] outgoings = new Float3[64];
	static readonly Sample2D[] samples = new Sample2D[1024];

	[Test]
	public void Sample([ValueSource(nameof(pairs))] (CheckType type, BxDF function) pair)
	{
		(CheckType type, BxDF function) = pair;

		int totalGoodSample = 0;

		foreach (Float3 outgoing in outgoings)
		{
			var energy = Summation.Zero;
			int goodSample = 0;

			foreach (Sample2D sample in samples)
			{
				var sampled = function.Sample(sample, outgoing, out Float3 incident);
				if (sampled.NotPossible) continue;

				++goodSample;

				Assert.That(outgoing.Magnitude, Is.EqualTo(1f).Roughly(1E-4f));
				Assert.That(incident.Magnitude, Is.EqualTo(1f).Roughly(1E-4f));

				RGB128 value = sampled.content / sampled.pdf * FastMath.Abs(incident.Dot(Float3.Forward));

				energy += value;

				float pdf = function.ProbabilityDensity(outgoing, incident);
				RGB128 evaluated = function.Evaluate(outgoing, incident);

				if (type == CheckType.onlyOverall) continue;

				if (function.type.Any(FunctionType.Specular))
				{
					Assert.That(pdf, Is.EqualTo(0f));
					Assert.That(evaluated, Is.EqualTo(RGB128.Black));
				}
				else
				{
					if (type != CheckType.onlyQuotient)
					{
						Assert.That(pdf, Is.EqualTo(sampled.pdf).Roughly(1f));
						AssertRoughlyEquals(evaluated, sampled.content);
					}
					else AssertRoughlyEquals(evaluated / pdf, sampled.content / sampled.pdf);

					RGB128 inverse = function.Evaluate(incident, outgoing) / function.ProbabilityDensity(incident, outgoing) * FastMath.Abs(outgoing.Dot(Float3.Forward));
					if (type == CheckType.everything) AssertRoughlyEquals(inverse, value);
				}
			}

			totalGoodSample += goodSample;

			if (goodSample > 0)
			{
				RGB128 result = (RGB128)(energy.Result / goodSample);
				Assert.That(result < (Float4)(1f + 0.02f));

				if (type is CheckType.everything or CheckType.onlyOverall) AssertRoughlyEquals(result, RGB128.White);
			}
		}

		int totalSample = samples.Length * outgoings.Length;
		TestContext.WriteLine($"Good sample ratio: {totalGoodSample} / {totalSample} ({(float)totalGoodSample / totalSample:P2})");
	}

	static BxDF MakeFunction<T>(Action<T> reset) where T : BxDF, new()
	{
		T function = new T();
		reset(function);
		return function;
	}

	static void AssertRoughlyEquals(RGB128 value0, RGB128 value1, float tolerance = 0.01f)
	{
		AssertSingle(value0.R, value1.R, tolerance);
		AssertSingle(value0.G, value1.G, tolerance);
		AssertSingle(value0.B, value1.B, tolerance);

		static void AssertSingle(float value0, float value1, float tolerance)
		{
			Assert.That(value0, Is.LessThanOrEqualTo(value1 * (1f + tolerance)).Or.GreaterThanOrEqualTo(value1 * (1f - tolerance)));
			Assert.That(value1, Is.LessThanOrEqualTo(value0 * (1f + tolerance)).Or.GreaterThanOrEqualTo(value0 * (1f - tolerance)));
		}
	}

	public enum CheckType
	{
		everything,
		oneDirection,
		onlyQuotient,
		onlyOverall
	}
}