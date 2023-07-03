using System.Collections.Generic;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Evaluation.Scattering;
using NUnit.Framework;

namespace Echo.UnitTests.Evaluation;

[TestFixture]
public class CoatedLambertianReflectionTests
{
	static CoatedLambertianReflectionTests()
	{
		inputs.Add(1f);
		inputs.Add(1.001f);
		inputs.Add(0.999f);
		inputs.Add(1.5f);
		inputs.Add(1.7f);
		inputs.Add(2f);
		inputs.Add(1f / 1.2f);
		inputs.Add(1f / 1.5f);

		var random = new SystemPrng(42);

		for (int i = 0; i < 50; i++) inputs.Add(random.Next1(0.6f, 2.2f));
		for (int i = 0; i < inputs.Count; i++) inputs[i] = 1f / inputs[i];
	}

	static readonly List<float> inputs = new();

	[Test]
	public void ReflectanceExact([ValueSource(nameof(inputs))] float eta)
	{
		float reflectance = CoatedLambertianReflection.FresnelDiffuseReflectance(eta);
		float converged = CoatedLambertianReflection.FresnelDiffuseReflectanceConverge(eta);

		Assert.That(reflectance, Is.Not.Negative);
		Assert.That(converged, Is.Not.Negative);

		float difference = reflectance - converged;
		float epsilon = FastMath.Abs(eta - 1f) < 1E-2f ? 1E-4f : 1E-6f;
		Assert.That(difference * difference, Is.LessThan(epsilon));
	}

	[Test]
	public void ReflectanceFast([ValueSource(nameof(inputs))] float eta)
	{
		float reflectance = CoatedLambertianReflection.FresnelDiffuseReflectanceFast(eta);
		float converged = CoatedLambertianReflection.FresnelDiffuseReflectanceConverge(eta);

		Assert.That(reflectance, Is.Not.Negative);
		Assert.That(converged, Is.Not.Negative);

		float difference = reflectance - converged;
		Assert.That(difference * difference, Is.LessThan(1E-6f));
	}
}