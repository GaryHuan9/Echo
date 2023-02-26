using System;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using NUnit.Framework;

namespace Echo.UnitTests.Aggregation;

[TestFixture]
public class SphereBoundTests
{
	[Test]
	public void ContainsAll([Values(1, 2, 3, 4, 5, 6, 100, 1000)] int count)
	{
		Prng random = Utility.NewRandom();
		Float3[] points = new Float3[count];
		float radius = random.Next1(count / 2f, count);

		for (int i = 0; i < count; i++) points[i] = random.Next3(radius);

		SphereBound bound = new SphereBound(points);
		foreach (Float3 point in points) Assert.That(bound.Contains(point));
	}

	[Test]
	[Repeat(1000)]
	public void Tightness([Values(64, 512)] int count)
	{
		Prng random = Utility.NewRandom();
		Float3[] points = new Float3[count];
		float radius = random.Next1(count / 2f, count);

		StratifiedDistribution distribution = new() { Extend = count, Prng = random };

		distribution.BeginSeries(Int2.Zero);

		for (int i = 0; i < count; i++)
		{
			distribution.BeginSession();
			Sample2D sample = distribution.Next2D();
			points[i] = sample.UniformSphere * radius;
		}

		SphereBound bound = new SphereBound(points);
		foreach (Float3 point in points) Assert.That(bound.Contains(point));

		//Generate new points that should excluded by our bound
		const float Allowance = 0.03f;
		radius *= 1f + Allowance;

		for (int i = 0; i < count; i++) Assert.That(!bound.Contains(random.NextOnSphere(radius)));
	}

	[Test]
	public void Edge()
	{
		//Created from a bug
		BoxBound box = new BoxBound(Float3.One * -3f, Float3.One * 3f);
		Span<Float3> points = stackalloc Float3[8];
		box.FillVertices(points);

		SphereBound bound = new SphereBound(points);
		foreach (Float3 point in points) Assert.That(bound.Contains(point));
	}
}