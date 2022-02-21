using EchoRenderer.Common.Mathematics.Randomization;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace EchoRenderer.UnitTests;

public static class Utilities
{
	/// <summary>
	/// Returns a new <see cref="IRandom"/> for this current <see cref="TestContext"/>.
	/// </summary>
	public static IRandom NewRandom() => new SquirrelRandom(TestContext.CurrentContext.Random.NextUInt());

	/// <inheritdoc cref="EqualConstraint.Ulps"/>
	public static EqualConstraint Roughly(this EqualConstraint constraint, int ulps = 3) => constraint.Within(ulps).Ulps;
}