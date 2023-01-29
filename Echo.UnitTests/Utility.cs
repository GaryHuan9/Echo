using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Randomization;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Echo.UnitTests;

public static class Utility
{
	/// <summary>
	/// Returns a new <see cref="Prng"/> for this current <see cref="TestContext"/>.
	/// </summary>
	public static Prng NewRandom() => new SquirrelPrng(TestContext.CurrentContext.Random.NextUInt());

	/// <inheritdoc cref="EqualConstraint.Ulps"/>
	public static EqualConstraint Roughly(this EqualConstraint constraint, int ulps = 10) => constraint.Within(ulps).Ulps;

	/// <inheritdoc cref="EqualConstraint.Percent"/>
	public static EqualConstraint Roughly(this EqualConstraint constraint, float percent) => constraint.Within(percent).Percent;

	/// <inheritdoc cref="EqualConstraint.Percent"/>
	public static ComparisonConstraint Roughly(this ComparisonConstraint constraint, float percent = 0.01f) => constraint.Within(percent).Percent;

	/// <summary>
	/// Returns a <see cref="RangeConstraint"/> that tests for the value to be almost zero.
	/// </summary>
	public static RangeConstraint AlmostZero(float epsilon = FastMath.Epsilon) => Is.InRange(-epsilon, epsilon);
}