using System;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Common.Mathematics;

/// <summary>
/// A class containing methods that can perform fast mathematical operations.
/// NOTE: all of the methods in this class should be automatically inlined.
/// </summary>
public static class FastMath
{
	static readonly float oneMinusEpsilon = Scalars.UInt32ToSingleBits(Scalars.SingleToUInt32Bits(1f) - 1u);

	//NOTE: some methods in this class is not necessarily "fast", however fast alternatives can be implemented later on if needed.

	/// <summary>
	/// Returns <paramref name="value"/> if it is larger than zero, or zero otherwise.
	/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is simply returned.
	/// </summary>
	public static float Max0(float value) => value < 0f ? 0f : value;

	/// <summary>
	/// Returns <paramref name="value"/> as if it is numerically clamped between zero and one.
	/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is simply returned.
	/// </summary>
	public static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;

	/// <summary>
	/// Returns <paramref name="value"/> as if it is numerically clamped between negative one and one.
	/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is simply returned.
	/// </summary>
	public static float Clamp11(float value) => value < -1f ? -1f : value > 1f ? 1f : value;

	/// <summary>
	/// Returns <paramref name="value"/> as if it is clamped between zero (inclusive) and one (exclusive).
	/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is simply returned.
	/// </summary>
	public static float ClampEpsilon(float value) => value < 0f ? 0f : value >= 1f ? oneMinusEpsilon : value;

	/// <summary>
	/// Returns the absolute value of <paramref name="value"/>.
	/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is simply returned.
	/// </summary>
	public static float Abs(float value) => value < 0f ? -value : value; //NaN is simply returned

	/// <summary>
	/// Returns the square root of <paramref name="value"/> if is larger than zero, or zero otherwise.
	/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is simply returned.
	/// </summary>
	public static float Sqrt0(float value) => value <= 0f ? 0f : MathF.Sqrt(value);

	/// <summary>
	/// Returns the inverse/reciprocal square root of <paramref name="value"/> if it is larger than zero.
	/// If <paramref name="value"/> is zero, <see cref="float.PositiveInfinity"/> is returned.
	/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is simply returned.
	/// </summary>
	public static float SqrtR0(float value) => value <= 0f ? float.PositiveInfinity : 1f / MathF.Sqrt(value);

	/// <summary>
	/// Returns either sine or cosine using the Pythagoras identity sin^2 + cos^2 = 1.
	/// The value returned is always positive, unlike regular trigonometric functions.
	/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is simply returned.
	/// </summary>
	public static float Identity(float value) => Sqrt0(FMA(value, -value, 1f));

	/// <summary>
	/// Computes and returns <paramref name="value"/> * <paramref name="multiplier"/> + <paramref name="adder"/> in one instruction.
	/// NOTE: while the performance benefit is nice, the main reason that we perform FMA operations is because of the better precision.
	/// </summary>
	public static float FMA(float value, float multiplier, float adder) => MathF.FusedMultiplyAdd(value, multiplier, adder);

	/// <summary>
	/// Computes and returns <paramref name="value"/> squared + <paramref name="adder"/> in one instruction.
	/// NOTE: this is only a shortcut for <see cref="FMA"/> and uses it internally to perform the operation.
	/// </summary>
	public static float FSA(float value, float adder) => FMA(value, value, adder);

	/// <summary>
	/// Calculates and outputs both the sine and cosine value of <paramref name="radians"/>.
	/// </summary>
	public static void SinCos(float radians, out float sin, out float cos) => (sin, cos) = MathF.SinCos(radians);

	/// <summary>
	/// Returns whether <paramref name="value"/> is positive based on <paramref name="epsilon"/>.
	/// </summary>
	public static bool Positive(float value, float epsilon = 1E-8f) => value > epsilon;

	/// <summary>
	/// Returns whether <paramref name="value"/> is almost zero based on <paramref name="epsilon"/>.
	/// </summary>
	public static bool AlmostZero(float value, float epsilon = 1E-8f) => -epsilon < value && value < epsilon;
}