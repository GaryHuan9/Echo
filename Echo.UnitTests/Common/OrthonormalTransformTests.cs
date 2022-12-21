using System.Collections.Generic;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Common.Packed;
using NUnit.Framework;

namespace Echo.UnitTests.Common;

[TestFixture]
public class OrthonormalTransformTests
{
	static OrthonormalTransformTests()
	{
		var random = new SystemPrng(42);

		for (int i = 0; i < 100; i++) vectors.Add(random.NextOnSphere());
	}

	static readonly List<Float3> vectors = new() { Float3.Right, Float3.Left, Float3.Up, Float3.Down, Float3.Forward, Float3.Backward };

	[Test]
	public void Correctness([ValueSource(nameof(vectors))] Float3 axisZ)
	{
		var transform = new OrthonormalTransform(axisZ);
		Assert.That(transform.axisZ, Is.EqualTo(axisZ));
		
		Assert.That(transform.axisX.SquaredMagnitude, Is.EqualTo(1f).Roughly());
		Assert.That(transform.axisY.SquaredMagnitude, Is.EqualTo(1f).Roughly());
		Assert.That(transform.axisZ.SquaredMagnitude, Is.EqualTo(1f).Roughly());
		
		Assert.That(transform.axisX.Dot(transform.axisY), Utility.AlmostZero());
		Assert.That(transform.axisY.Dot(transform.axisZ), Utility.AlmostZero());
		Assert.That(transform.axisZ.Dot(transform.axisX), Utility.AlmostZero());
		
		AssertAlmostEquals(transform.axisX.Cross(transform.axisY), transform.axisZ);
		AssertAlmostEquals(transform.axisY.Cross(transform.axisZ), transform.axisX);
		AssertAlmostEquals(transform.axisZ.Cross(transform.axisX), transform.axisY);

		foreach (Float3 oldValue in vectors)
		{
			Float3 newValue = transform.ApplyForward(oldValue);
			float oldAngle = Float3.Angle(oldValue, Float3.Forward);
			float newAngle = Float3.Angle(newValue, transform.axisZ);
			
			Assert.That(oldAngle, Is.EqualTo(newAngle).Roughly());
			AssertAlmostEquals(transform.ApplyInverse(newValue), oldValue);
		}
	}

	static void AssertAlmostEquals(in Float3 value0, in Float3 value1)
	{
		Assert.That(Float3.SquaredDistance(value0, value1), Utility.AlmostZero());
	}
}