using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using NUnit.Framework;

namespace EchoRenderer.UnitTests;

public class BoundingSphereTest
{
	[SetUp]
	public void Setup()
	{
		GetRandomPoints(500, 5, out points);
		boundSphere = new BoundingSphere(points);
	}

	[Test]
	public void TestContained()
	{
		foreach (Float3 point in points)
		{
			Assert.That(
				boundSphere.Contains(point),
				$"BoundSphere [{boundSphere.center} : {boundSphere.radius}] doesn't contain point {point}"
			);
		}
	}

	readonly Random random = new();

	Float3[] points;

	BoundingSphere boundSphere;
	
	void GetRandomPoints(int amount, float size, out Float3[] points)
	{
		points = new Float3[amount];
		
		for (int i = 0; i < amount; ++i)
		{
			points[i] = GetRandomPoint(size);
		}
	}

	Float3 GetRandomPoint(float size) =>
		new Float3(
			random.NextSingle() * size,
			random.NextSingle() * size,
			random.NextSingle() * size
		);
}