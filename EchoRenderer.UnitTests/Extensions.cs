using NUnit.Framework.Constraints;

namespace EchoRenderer.UnitTests;

public static class Extensions
{
	/// <inheritdoc cref="EqualConstraint.Ulps"/>
	public static EqualConstraint Roughly(this EqualConstraint constraint, int ulps = 3) => constraint.Within(ulps).Ulps;
}