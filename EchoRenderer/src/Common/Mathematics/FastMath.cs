using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Common.Mathematics;

/// <summary>
/// A class containing methods that can perform fast mathematical operations.
/// </summary>
public static class FastMath
{
	static FastMath() => Assert.AreEqual(Scalars.SingleToUInt32Bits(OneMinusEpsilon), Scalars.SingleToUInt32Bits(1f) - 1u);

	//NOTE: Some naming conversions used in this project:
	//R = reciprocal (eg: three = 3f -> threeR = 1f / three = 1f / 3f)
	//2 = the square (eg: three = 3f -> three2 = three * three = 9f)

	/// <summary>
	/// This is the largest IEEE-754 float32 value that is smaller than 1f (ie. 1f - 1ulp).
	/// </summary>
	const float OneMinusEpsilon = 0.99999994f;

	const MethodImplOptions Options = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;

	//NOTE: some methods in this class is not necessarily "fast" yet, however fast alternatives can be implemented later on if needed.

	/// <summary>
	/// Returns <paramref name="value"/> if it is larger than zero, or zero otherwise.
	/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.
	/// </summary>
	[MethodImpl(Options)]
	public static float Max0(float value) => value < 0f ? 0f : value;

	/// <summary>
	/// Returns <paramref name="value"/> as if it is numerically clamped between zero and one.
	/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.
	/// </summary>
	[MethodImpl(Options)]
	public static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;

	/// <summary>
	/// Returns <paramref name="value"/> as if it is numerically clamped between negative one and one.
	/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.
	/// </summary>
	[MethodImpl(Options)]
	public static float Clamp11(float value) => value < -1f ? -1f : value > 1f ? 1f : value;

	/// <summary>
	/// Returns <paramref name="value"/> as if it is clamped between zero (inclusive) and one (exclusive).
	/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.
	/// </summary>
	[MethodImpl(Options)]
	public static float ClampEpsilon(float value) => value < 0f ? 0f : value >= 1f ? OneMinusEpsilon : value;

	/// <summary>
	/// Returns the absolute value of <paramref name="value"/>.
	/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.
	/// </summary>
	[MethodImpl(Options)]
	public static float Abs(float value) => value < 0f ? -value : value;

	/// <summary>
	/// Returns the square root of <paramref name="value"/> if is larger than zero, or zero otherwise.
	/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.
	/// </summary>
	[MethodImpl(Options)]
	public static float Sqrt0(float value) => value <= 0f ? 0f : MathF.Sqrt(value);

	/// <summary>
	/// Returns the inverse/reciprocal square root of <paramref name="value"/> if it is
	/// larger than zero. Otherwise, <see cref="float.PositiveInfinity"/> is returned.
	/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.
	/// </summary>
	[MethodImpl(Options)]
	public static float SqrtR0(float value) => value <= 0f ? float.PositiveInfinity : 1f / MathF.Sqrt(value);

	/// <summary>
	/// Returns one minus <paramref name="value"/> squared using just one FMA instruction.
	/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.
	/// </summary>
	[MethodImpl(Options)]
	public static float OneMinus2(float value) => FMA(value, -value, 1f);

	/// <summary>
	/// Returns either sine or cosine using the Pythagoras identity sin^2 + cos^2 = 1.
	/// The value returned is always positive, unlike regular trigonometric functions.
	/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.
	/// </summary>
	[MethodImpl(Options)]
	public static float Identity(float value) => Sqrt0(OneMinus2(value));

	/// <summary>
	/// Computes and returns <paramref name="value"/> * <paramref name="multiplier"/> + <paramref name="adder"/> in one instruction.
	/// NOTE: while the performance benefit is nice, the main reason that we perform FMA operations is because of the better precision.
	/// </summary>
	[MethodImpl(Options)]
	public static float FMA(float value, float multiplier, float adder) => MathF.FusedMultiplyAdd(value, multiplier, adder);

	/// <summary>
	/// Computes and returns <paramref name="value"/> squared + <paramref name="adder"/> in one instruction.
	/// NOTE: this is only a shortcut for <see cref="FMA"/> and uses it internally to perform the operation.
	/// </summary>
	[MethodImpl(Options)]
	public static float F2A(float value, float adder) => FMA(value, value, adder);

	/// <summary>
	/// Calculates and outputs both the sine and cosine value of <paramref name="radians"/>.
	/// </summary>
	[MethodImpl(Options)]
	public static void SinCos(float radians, out float sin, out float cos) => (sin, cos) = MathF.SinCos(radians);

	/// <summary>
	/// Returns whether <paramref name="value"/> is positive based on <paramref name="epsilon"/>.
	/// </summary>
	[MethodImpl(Options)]
	public static bool Positive(float value, float epsilon = 1E-8f) => value > epsilon;

	/// <summary>
	/// Returns whether <paramref name="value"/> is almost zero based on <paramref name="epsilon"/>.
	/// </summary>
	[MethodImpl(Options)]
	public static bool AlmostZero(float value, float epsilon = 1E-8f) => (-epsilon < value) & (value < epsilon);
}