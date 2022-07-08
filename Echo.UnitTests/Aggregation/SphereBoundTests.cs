using CodeHelpers.Packed;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Common.Mathematics.Randomization;
using NUnit.Framework;

namespace Echo.UnitTests.Aggregation;

[TestFixture]
public class SphereBoundTests
{
	[Test]
	public void ContainsAll([Values(1, 10, 100, 1000)] int pointsCount)
	{
		var points = GenerateRandomPoints(pointsCount);
		var bound = new SphereBound(points);

		Assert.That(points, Is.All.Matches<Float3>(point => bound.Contains(point)));
	}

	static Float3[] GenerateRandomPoints(int pointsCount)
	{
		Prng random = Utility.NewRandom();

		var points = new Float3[pointsCount];
		float maxRadius = random.Next1();

		for (int i = 0; i < pointsCount; i++)
		{
			points[i] = random.NextInSphere(maxRadius);
		}

		return points;
	}
}