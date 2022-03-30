using System;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Common.Mathematics.Randomization;
using NUnit.Framework;

namespace EchoRenderer.UnitTests;

[TestFixture]
public class BoundingSphereTests
{
	[SetUp]
	public void Setup()
	{
		points = GenerateRandomPoints();
		sphere = new BoundingSphere(points);
	}

	[Test]
	public void ContainsAll()
	{
		foreach (Float3 point in points) Assert.That(sphere.Contains(point));
	}

	Float3[] points;
	BoundingSphere sphere;

	static Float3[] GenerateRandomPoints()
	{
		IRandom random = Utilities.NewRandom();

		var points = new Float3[random.Next1(1000)];
		FillRandom(points, random.Next1(), random);

		return points;
	}

	static void FillRandom(Span<Float3> points, float maxRadius, IRandom random)
	{
		foreach (ref Float3 point in points) point = random.NextInSphere(maxRadius);
	}
}