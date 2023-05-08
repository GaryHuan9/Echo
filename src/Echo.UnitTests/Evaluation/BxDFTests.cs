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

	static readonly (bool conservative, BxDF function)[] pairs =
	{
		(true, MakeFunction<LambertianReflection>(_ => { })),
		(true, MakeFunction<LambertianTransmission>(_ => { })),
		(false, MakeFunction<OrenNayar>(bxdf => bxdf.Reset(0.15f))),
		(false, MakeFunction<OrenNayar>(bxdf => bxdf.Reset(0.82f))),
		(false, MakeFunction<SpecularReflection<RealFresnel>>(bxdf => bxdf.Reset(new RealFresnel(1.1f, 1.7f)))),
		(false, MakeFunction<SpecularReflection<RealFresnel>>(bxdf => bxdf.Reset(new RealFresnel(1.7f, 1.1f)))),
		(false, MakeFunction<SpecularReflection<ComplexFresnel>>(bxdf => bxdf.Reset(new ComplexFresnel(new RGB128(0.9f, 1.1f, 1.2f), new RGB128(0.27105f, 0.67693f, 1.31640f), new RGB128(3.60920f, 2.62480f, 2.29210f))))), //Copper
		(false, MakeFunction<SpecularReflection<ComplexFresnel>>(bxdf => bxdf.Reset(new ComplexFresnel(new RGB128(1.3f, 1.0f, 0.8f), new RGB128(2.74070f, 2.54180f, 2.26700f), new RGB128(3.81430f, 3.43450f, 3.03850f))))), //Titanium
		(false, MakeFunction<SpecularReflection<ComplexFresnel>>(bxdf => bxdf.Reset(new ComplexFresnel(RGB128.White, RGB128.MaxEpsilon(RGB128.Black), RGB128.Black)))),
		(false, MakeFunction<SpecularTransmission>(bxdf => bxdf.Reset(new RealFresnel(1.1f, 1.7f)))),
		(false, MakeFunction<SpecularTransmission>(bxdf => bxdf.Reset(new RealFresnel(1.7f, 1.1f)))),
		(true, MakeFunction<SpecularFresnel>(bxdf => bxdf.Reset(new RealFresnel(1.1f, 1.7f)))),
		(true, MakeFunction<SpecularFresnel>(bxdf => bxdf.Reset(new RealFresnel(1.7f, 1.1f)))),
		(false, MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, RealFresnel>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(0.3f, 0.8f), new RealFresnel(1.1f, 1.7f)))),
		(false, MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, RealFresnel>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(0.7f, 0.4f), new RealFresnel(1.7f, 1.1f)))),
		(false, MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, RealFresnel>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(1f, 1f), new RealFresnel(1.0f, 1.5f)))),
		(false, MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, RealFresnel>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(1E-4f, 1E-4f), new RealFresnel(1.0f, 1.5f)))),
		(false, MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, ComplexFresnel>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(0.3f, 0.8f), new ComplexFresnel(new RGB128(0.9f, 1.1f, 1.2f), new RGB128(0.27105f, 0.67693f, 1.31640f), new RGB128(3.60920f, 2.62480f, 2.29210f))))), //Copper
		(false, MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, ComplexFresnel>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(0.7f, 0.4f), new ComplexFresnel(new RGB128(1.3f, 1.0f, 0.8f), new RGB128(2.74070f, 2.54180f, 2.26700f), new RGB128(3.81430f, 3.43450f, 3.03850f))))), //Titanium
		(false, MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, ComplexFresnel>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(1f, 1f), new ComplexFresnel(RGB128.White, RGB128.MaxEpsilon(RGB128.Black), RGB128.Black)))),
		(false, MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, ComplexFresnel>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(1E-4f, 1E-4f), new ComplexFresnel(RGB128.White, RGB128.White * 2f, RGB128.White * 3f)))),
		(false, MakeFunction<GlossyTransmission<TrowbridgeReitzMicrofacet>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(0.3f, 0.8f), new RealFresnel(1.1f, 1.7f)))),
		(false, MakeFunction<GlossyTransmission<TrowbridgeReitzMicrofacet>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(0.7f, 0.4f), new RealFresnel(1.7f, 1.1f)))),
		(false, MakeFunction<GlossyTransmission<TrowbridgeReitzMicrofacet>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(1f, 1f), new RealFresnel(1.0f, 1.5f)))),
		(false, MakeFunction<GlossyTransmission<TrowbridgeReitzMicrofacet>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(1E-4f, 1E-4f), new RealFresnel(1.0f, 1.5f))))
	};

	static readonly Float3[] outgoings = new Float3[64];
	static readonly Sample2D[] samples = new Sample2D[1024];

	[Test]
	public void Sample([ValueSource(nameof(pairs))] (bool conservative, BxDF function) pair)
	{
		(bool conservative, BxDF function) = pair;

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

				Assert.That(outgoing.Magnitude, Is.EqualTo(1f).Roughly());
				Assert.That(incident.Magnitude, Is.EqualTo(1f).Roughly());

				RGB128 value = sampled.content / sampled.pdf * FastMath.Abs(incident.Dot(Float3.Forward));

				energy += value;

				float pdf = function.ProbabilityDensity(outgoing, incident);
				RGB128 evaluated = function.Evaluate(outgoing, incident);

				if (function.type.Any(FunctionType.Specular))
				{
					Assert.That(pdf, Is.EqualTo(0f));
					Assert.That(evaluated, Is.EqualTo(RGB128.Black));
				}
				else
				{
					Assert.That(pdf, Is.EqualTo(sampled.pdf).Roughly(1f));
					AssertRoughlyEquals(evaluated, sampled.content);

					RGB128 inverse = function.Evaluate(incident, outgoing) / function.ProbabilityDensity(incident, outgoing) * FastMath.Abs(outgoing.Dot(Float3.Forward));
					if (conservative) AssertRoughlyEquals(inverse, value);
				}
			}

			totalGoodSample += goodSample;

			if (goodSample > 0)
			{
				RGB128 result = (RGB128)(energy.Result / goodSample);
				Assert.That(result < (Float4)(1f + 0.02f));

				if (conservative) AssertRoughlyEquals(result, RGB128.White);
			}
		}

		int totalSample = samples.Length * outgoings.Length;
		TestContext.WriteLine($"Good sample ratio: {totalGoodSample} / {totalSample} ({(float)totalGoodSample / totalSample:P2})");
	}

	static BxDF MakeFunction<T>(Action<T> reset) where T : BxDF, new()
	{
		T bxdf = new T();
		reset(bxdf);
		return bxdf;
	}

	static void AssertRoughlyEquals(RGB128 value0, RGB128 value1, float tolerance = 0.01f)
	{
		Float4 converted0 = value0.ToRGBA128().AlphaOne;
		Float4 converted1 = value1.ToRGBA128().AlphaOne;

		Assert.That(converted0 > converted1 * (1f - tolerance));
		Assert.That(converted0 < converted1 * (1f + tolerance));
	}
}