using System.Collections.Generic;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Scenic.Geometries;
using NUnit.Framework;

namespace Echo.UnitTests.Scenic;

[TestFixture]
public class PreparedSphereTests
{
	static PreparedSphereTests()
	{
		var random = new SystemPrng(42);

		spheres.Add(new PreparedSphere(Float3.Zero, 1f, default));
		spheres.Add(new PreparedSphere(Float3.One, 0.01f, default));
		spheres.Add(new PreparedSphere(Float3.NegativeHalf, 2f, default));
		spheres.Add(new PreparedSphere(new Float3(4f, -1f, 2f), 347f, default));

		for (int i = 0; i < 20; i++) spheres.Add(new PreparedSphere(random.NextInSphere(10f), random.Next1(1f, 10f), default));

		rays.Add(new Ray(Float3.Zero, Float3.Up));
		rays.Add(new Ray(Float3.Backward, Float3.Forward));
		rays.Add(new Ray(Float3.One, Float3.NegativeOne.Normalized));
		rays.Add(new Ray(new Float3(1f, 0f, 1f), Float3.Backward));
		rays.Add(new Ray(new Float3(1f, 1E-5f, 1E-7f), Float3.Left));

		for (int i = 0; i < 70; i++)
		{
			Float3 origin = random.NextInSphere(100f);
			rays.Add(new Ray(origin, (random.NextInSphere(10f) - origin).Normalized));
		}
	}

	static readonly List<PreparedSphere> spheres = new();
	static readonly List<Ray> rays = new();

	[Test]
	public void IntersectFloat([ValueSource(nameof(spheres))] PreparedSphere sphere,
							   [ValueSource(nameof(rays))] Ray ray)
	{
		float close = sphere.Intersect(ray, out _);
		float far = sphere.Intersect(ray, out _, true);
		float closeReference = RayMarch(sphere, ray);
		float farReference = closeReference;

		if (farReference < PreparedSphere.DistanceThreshold)
		{
			//Find the farther intersection
			float distance = closeReference + sphere.radius * 4f;
			Ray reversed = new Ray(ray.GetPoint(distance), -ray.direction);
			farReference = distance - RayMarch(sphere, reversed);
		}

		Assert.That(close, Is.EqualTo(closeReference).Roughly(0.1f));
		Assert.That(far, Is.EqualTo(farReference).Roughly(0.1f));
	}

	[Test]
	public void IntersectBool([ValueSource(nameof(spheres))] PreparedSphere sphere,
							  [ValueSource(nameof(rays))] Ray ray)
	{
		float travel = ray.origin.Magnitude;

		bool close = sphere.Intersect(ray, travel);
		bool far = sphere.Intersect(ray, travel, true);
		bool closeReference = sphere.Intersect(ray, out _) < travel;
		bool farReference = sphere.Intersect(ray, out _, true) < travel;

		Assert.AreEqual(close, closeReference);
		Assert.AreEqual(far, farReference);
	}

	[Test]
	public void Sample([ValueSource(nameof(spheres))] PreparedSphere sphere,
					   [ValueSource(nameof(rays))] Ray ray)
	{
		Float3 origin = ray.origin;

		var distribution = new StratifiedDistribution
		{
			Extend = 100,
			Prng = new SystemPrng(42)
		};

		distribution.BeginSeries(Int2.Zero);

		for (int i = 0; i < distribution.Extend; i++)
		{
			distribution.BeginSession();

			(var point, float sampledPdf) = sphere.Sample(origin, distribution.Next2D());
			Assert.That(sampledPdf, Is.Not.Zero);
			Assert.That(point.normal.SquaredMagnitude, Is.EqualTo(1f).Roughly());
			Assert.That(point.position, Is.EqualTo(sphere.position + sphere.radius * point.normal));

			if (point.position == origin) continue;

			Float3 incident = (point.position - origin).Normalized;
			float pdf = sphere.ProbabilityDensity(origin, incident);

			Assert.That(pdf, Is.Not.Zero);
			Assert.That(pdf, Is.EqualTo(sampledPdf).Roughly(0.1f));
		}
	}

	static float RayMarch(in PreparedSphere sphere, in Ray ray)
	{
		bool inside = Float3.Distance(sphere.position, ray.origin) <= sphere.radius;

		double travel = 0d;

		while (true)
		{
			double distance = Float3.Distance(ray.GetPoint((float)travel), sphere.position) - (double)sphere.radius;
			if (inside) distance = -distance;

			if (distance <= 0f) break;
			travel += distance * 0.9d;

			if (float.IsPositiveInfinity((float)travel)) break;
		}

		return (float)travel;
	}
}