using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Echo.Core.Common.Mathematics;

namespace Echo.Core.Common.Diagnostics;

public static class Ensure
{
	const string Symbol = "DEBUG";

	[Conditional(Symbol)]
	public static void IsNotNull<T>(T target) where T : class
	{
		if (target != null) return;
		throw new Exception("Target is null!");
	}

	[Conditional(Symbol)]
	public static void IsNull<T>(T target) where T : class
	{
		if (target == null) return;
		throw new Exception("Target is not null!");
	}

	[Conditional(Symbol)]
	public static void IsTrue([DoesNotReturnIf(false)] bool target)
	{
		if (target) return;
		throw new Exception("Target is not true!");
	}

	[Conditional(Symbol)]
	public static void IsFalse([DoesNotReturnIf(true)] bool target)
	{
		if (!target) return;
		throw new Exception("Target is not false!");
	}

	[Conditional(Symbol)]
	public static void AreEqual<T>(T target, T other)
	{
		if (AreEqualInternal(target, other)) return;
		throw new Exception("Target and other are not equal!");
	}

	[Conditional(Symbol)]
	public static void AreNotEqual<T>(T target, T other)
	{
		if (!AreEqualInternal(target, other)) return;
		throw new Exception("Target and other are equal!");
	}

	static bool AreEqualInternal<T>(T target, T other) => target switch
	{
		float value  => value.AlmostEquals((float)(object)other),
		double value => value.AlmostEquals((double)(object)other),
		_            => EqualityComparer<T>.Default.Equals(target, other)
	};
}