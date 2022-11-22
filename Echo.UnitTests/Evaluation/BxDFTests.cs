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
			Extend = samples.Length,
			Prng = new SystemPrng(1)
		};

		distribution.BeginSeries(Int2.Zero);
		distribution.BeginSession();

		foreach (ref Float3 direction in outgoings.AsSpan()) direction = distribution.Next2D().UniformSphere;

		distribution = new StratifiedDistribution
		{
			Extend = outgoings.Length,
			Prng = new SystemPrng(2)
		};

		distribution.BeginSeries(Int2.Zero);
		distribution.BeginSession();

		foreach (ref Sample2D sample in samples.AsSpan()) sample = distribution.Next2D();
	}

	static readonly BxDF[] functions =
	{
		MakeFunction<LambertianReflection>(_ => { }),
		MakeFunction<LambertianTransmission>(_ => { }),
		MakeFunction<OrenNayar>(bxdf => bxdf.Reset(0.15f)),
		MakeFunction<OrenNayar>(bxdf => bxdf.Reset(0.82f)),
		MakeFunction<SpecularReflection<RealFresnel>>(bxdf => bxdf.Reset(new RealFresnel(1.1f, 1.7f))),
		MakeFunction<SpecularReflection<RealFresnel>>(bxdf => bxdf.Reset(new RealFresnel(1.7f, 1.1f))),
		MakeFunction<SpecularReflection<ComplexFresnel>>(bxdf => bxdf.Reset(new ComplexFresnel(new RGB128(0.9f, 1.1f, 1.2f), new RGB128(0.27105f, 0.67693f, 1.31640f), new RGB128(3.60920f, 2.62480f, 2.29210f)))), //Copper
		MakeFunction<SpecularReflection<ComplexFresnel>>(bxdf => bxdf.Reset(new ComplexFresnel(new RGB128(1.3f, 1.0f, 0.8f), new RGB128(2.74070f, 2.54180f, 2.26700f), new RGB128(3.81430f, 3.43450f, 3.03850f)))), //Titanium
		MakeFunction<SpecularTransmission>(bxdf => bxdf.Reset(new RealFresnel(1.1f, 1.7f))),
		MakeFunction<SpecularTransmission>(bxdf => bxdf.Reset(new RealFresnel(1.7f, 1.1f))),
		MakeFunction<SpecularFresnel>(bxdf => bxdf.Reset(new RealFresnel(1.1f, 1.7f))),
		MakeFunction<SpecularFresnel>(bxdf => bxdf.Reset(new RealFresnel(1.7f, 1.1f))),
		MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, RealFresnel>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(0.3f, 0.8f), new RealFresnel(1.1f, 1.7f))),
		MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, RealFresnel>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(0.7f, 0.4f), new RealFresnel(1.7f, 1.1f))),
		MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, ComplexFresnel>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(0.3f, 0.8f), new ComplexFresnel(new RGB128(0.9f, 1.1f, 1.2f), new RGB128(0.27105f, 0.67693f, 1.31640f), new RGB128(3.60920f, 2.62480f, 2.29210f)))), //Copper
		MakeFunction<GlossyReflection<TrowbridgeReitzMicrofacet, ComplexFresnel>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(0.7f, 0.4f), new ComplexFresnel(new RGB128(1.3f, 1.0f, 0.8f), new RGB128(2.74070f, 2.54180f, 2.26700f), new RGB128(3.81430f, 3.43450f, 3.03850f)))), //Titanium
		MakeFunction<GlossyTransmission<TrowbridgeReitzMicrofacet>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(0.3f, 0.8f), new RealFresnel(1.1f, 1.7f))),
		MakeFunction<GlossyTransmission<TrowbridgeReitzMicrofacet>>(bxdf => bxdf.Reset(new TrowbridgeReitzMicrofacet(0.7f, 0.4f), new RealFresnel(1.7f, 1.1f)))
	};

	static readonly Float3[] outgoings = new Float3[64];
	static readonly Sample2D[] samples = new Sample2D[1024];

	[Test]
	public void Sample([ValueSource(nameof(functions))] BxDF function)
	{
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

				if (function.type.Any(FunctionType.Specular)) continue;

				float pdf = function.ProbabilityDensity(outgoing, incident);
				RGB128 evaluated = function.Evaluate(outgoing, incident);

				Assert.That(pdf, Is.EqualTo(sampled.pdf).Roughly());
				Assert.That(evaluated, Is.EqualTo(sampled.content));

				RGB128 inverseEvaluated = function.Evaluate(incident, outgoing);

				RGB128 inverse = inverseEvaluated / function.ProbabilityDensity(incident, outgoing) * FastMath.Abs(outgoing.Dot(Float3.Forward));
				Assert.That(inverse, Is.EqualTo(value));
			}

			totalGoodSample += goodSample;
			Assert.That(energy.Result / goodSample <= (Float4)1.02f); //2% error allowed on all channels
		}

		int totalSample = samples.Length * outgoings.Length;
		TestContext.Out.Write($"Good sample ratio: {totalGoodSample} / {totalSample} ({(float)totalGoodSample / totalSample:P2})");
	}

	static BxDF MakeFunction<T>(Action<T> reset) where T : BxDF, new()
	{
		T bxdf = new T();
		reset(bxdf);
		return bxdf;
	}
}