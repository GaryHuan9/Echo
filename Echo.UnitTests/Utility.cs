using Echo.Common.Mathematics.Randomization;
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
	public static EqualConstraint Roughly(this EqualConstraint constraint, int ulps = 3) => constraint.Within(ulps).Ulps;

	/// <inheritdoc cref="EqualConstraint.Percent"/>
	public static EqualConstraint Roughly(this EqualConstraint constraint, float percent) => constraint.Within(percent).Percent;
}