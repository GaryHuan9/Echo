using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Echo.Core.Common.Diagnostics;

namespace Echo.Core.Common.Mathematics;

/// <summary>
/// A class containing methods that can perform fast mathematical operations.
/// </summary>
public static class FastMath
{
	//This preprocessor is quite important, otherwise the JIT compiler will add garbage inside all of the static methods
#if !RELEASE
	static FastMath()
	{
		uint bits0 = BitConverter.SingleToUInt32Bits(OneMinusEpsilon);
		uint bits1 = BitConverter.SingleToUInt32Bits(1f) - 1u;
		Ensure.AreEqual(bits0, bits1);
	}
#endif

	//NOTE: Some naming conversions used in this project:
	//R = reciprocal    (eg: three = 3f -> threeR = 1f / three = 1f / 3f)
	//2 = the square    (eg: three = 3f -> three2 = three * three = 9f)
	//V = packed vector (eg. three = 3f -> threeV = <3f, 3f, 3f, 3f>)

	/// <summary>
	/// A positive number that is really small and close to zero.
	/// </summary>
	public const float Epsilon = 8E-7f;

	/// <summary>
	/// The largest IEEE-754 float32 value that is smaller than 1f (ie. 1f - 1ulp).
	/// </summary>
	public const float OneMinusEpsilon = 0.99999994f;

	const MethodImplOptions Options = MethodImplOptions.AggressiveInlining;

	//NOTE: some methods in this class is not necessarily "fast" yet, however fast alternatives can be implemented later on if needed.

	/// <summary>
	/// Returns <paramref name="value"/> if it is larger than zero, or zero otherwise.
	/// </summary>
	/// <remarks>If <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.</remarks>
	[MethodImpl(Options)]
	public static float Max0(float value)
	{
		Vector128<float> valueV = Vector128.CreateScalarUnsafe(value);
		return Sse.MaxScalar(Vector128<float>.Zero, valueV).ToScalar();
	}

	/// <summary>
	/// Returns <paramref name="value0"/> if it is smaller than <paramref name="value1"/>, otherwise <paramref name="value1"/> is returned.
	/// </summary>
	/// <remarks>If <paramref name="value0"/> or <paramref name="value1"/> is <see cref="float.NaN"/>, <paramref name="value1"/> is returned</remarks>
	[MethodImpl(Options)]
	public static float Min(float value0, float value1)
	{
		Vector128<float> value0V = Vector128.CreateScalarUnsafe(value0);
		Vector128<float> value1V = Vector128.CreateScalarUnsafe(value1);
		return Sse.MinScalar(value0V, value1V).ToScalar();
	}

	/// <summary>
	/// Returns <paramref name="value0"/> if it is larger than <paramref name="value1"/>, otherwise <paramref name="value1"/> is returned.
	/// </summary>
	/// <remarks>If <paramref name="value0"/> or <paramref name="value1"/> is <see cref="float.NaN"/>, <paramref name="value1"/> is returned</remarks>
	[MethodImpl(Options)]
	public static float Max(float value0, float value1)
	{
		Vector128<float> value0V = Vector128.CreateScalarUnsafe(value0);
		Vector128<float> value1V = Vector128.CreateScalarUnsafe(value1);
		return Sse.MaxScalar(value0V, value1V).ToScalar();
	}

	/// <summary>
	/// Returns <paramref name="value"/> as if it is numerically clamped between zero and one.
	/// </summary>
	/// <remarks>If <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.</remarks>
	[MethodImpl(Options)]
	public static float Clamp01(float value)
	{
		Vector128<float> valueV = Vector128.CreateScalarUnsafe(value);
		Vector128<float> min = Vector128.CreateScalarUnsafe(1f);
		valueV = Sse.MaxScalar(Vector128<float>.Zero, valueV);
		return Sse.MinScalar(min, valueV).ToScalar();
	}

	/// <summary>
	/// Returns <paramref name="value"/> as if it is numerically clamped between negative one and one.
	/// </summary>
	/// <remarks>If <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.</remarks>
	[MethodImpl(Options)]
	public static float Clamp11(float value)
	{
		Vector128<float> valueV = Vector128.CreateScalarUnsafe(value);
		Vector128<float> max = Vector128.CreateScalarUnsafe(-1f);
		Vector128<float> min = Vector128.CreateScalarUnsafe(1f);
		valueV = Sse.MaxScalar(max, valueV);
		return Sse.MinScalar(min, valueV).ToScalar();
	}

	/// <summary>
	/// Returns <paramref name="value"/> as if it is clamped between zero (inclusive) and one (exclusive).
	/// </summary>
	/// <remarks>If <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.</remarks>
	[MethodImpl(Options)]
	public static float ClampEpsilon(float value)
	{
		Vector128<float> valueV = Vector128.CreateScalarUnsafe(value);
		Vector128<float> min = Vector128.CreateScalarUnsafe(OneMinusEpsilon);
		valueV = Sse.MaxScalar(Vector128<float>.Zero, valueV);
		return Sse.MinScalar(min, valueV).ToScalar();
	}

	/// <summary>
	/// Returns the absolute value of <paramref name="value"/>.
	/// </summary>
	/// <remarks>If <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.</remarks>
	[MethodImpl(Options)]
	public static float Abs(float value)
	{
		Vector128<float> valueV = Vector128.CreateScalarUnsafe(value);
		Vector128<uint> mask = Vector128<uint>.AllBitsSet;
		mask = Sse2.ShiftRightLogical(mask, 1);
		return Sse.And(valueV, mask.AsSingle()).ToScalar();
	}

	/// <summary>
	/// Returns the square root of <paramref name="value"/> if is larger than zero, or zero otherwise.
	/// </summary>
	/// <remarks>If <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.</remarks>
	[MethodImpl(Options)]
	public static float Sqrt0(float value)
	{
		Vector128<float> zero = Vector128<float>.Zero;
		Vector128<float> valueV = Vector128.CreateScalarUnsafe(value);
		Vector128<float> mask = Sse.CompareScalarLessThanOrEqual(valueV, zero);
		return Sse.AndNot(mask, Sse.SqrtScalar(valueV)).ToScalar();
	}

	/// <summary>
	/// Returns the inverse/reciprocal square root of <paramref name="value"/> if it is
	/// larger than zero. Otherwise, <see cref="float.PositiveInfinity"/> is returned.
	/// </summary>
	/// <remarks>If <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.</remarks>
	[MethodImpl(Options)]
	public static float SqrtR0(float value) => 1f / Sqrt0(value);

	/// <summary>
	/// Returns one minus <paramref name="value"/> squared.
	/// </summary>
	/// <remarks>If <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.</remarks>
	[MethodImpl(Options)]
	public static float OneMinus2(float value)
	{
		if (!Fma.IsSupported) return 1f - value * value;
		Vector128<float> valueV = Vector128.CreateScalarUnsafe(value);
		Vector128<float> one = Vector128.CreateScalarUnsafe(1f);
		return Fma.MultiplyAddNegatedScalar(valueV, valueV, one).ToScalar();
	}

	/// <summary>
	/// Returns either sine or cosine using the Pythagoras identity sin^2 + cos^2 = 1.
	/// If <paramref name="value"/> is out of range (-1 to 1), then zero is returned.
	/// The value returned is always nonnegative, unlike regular trigonometric functions.
	/// </summary>
	/// <remarks>If <paramref name="value"/> is <see cref="float.NaN"/>, it is passed through.</remarks>
	[MethodImpl(Options)]
	public static float Identity(float value) => Sqrt0(OneMinus2(value));

	/// <summary>
	/// Computes and returns <paramref name="value"/> * <paramref name="multiplier"/> + <paramref name="adder"/>.
	/// </summary>
	/// <remarks>
	/// This method does not guarantee accuracy, unlike <see cref="MathF.FusedMultiplyAdd"/>; it only promises a potential performance boost.
	/// If accuracy is important, use <see cref="MathF.FusedMultiplyAdd"/>, otherwise if speed is important, use this method.
	/// </remarks>
	[MethodImpl(Options)]
	public static float FMA(float value, float multiplier, float adder)
	{
		if (!Fma.IsSupported) return value * multiplier + adder;
		return MathF.FusedMultiplyAdd(value, multiplier, adder);
	}

	/// <summary>
	/// Calculates and outputs both the sine and cosine value of <paramref name="radians"/>.
	/// </summary>
	[MethodImpl(Options)]
	public static void SinCos(float radians, out float sin, out float cos)
	{
		if (Abs(radians) > 1024f)
		{
			sin = MathF.Sin(radians);
			cos = MathF.Cos(radians);
		}
		else (sin, cos) = MathF.SinCos(radians);
	}

	/// <summary>
	/// Returns whether <paramref name="value"/> is positive based on <paramref name="epsilon"/>.
	/// </summary>
	[MethodImpl(Options)]
	public static bool Positive(float value, float epsilon = Epsilon) => epsilon <= value;

	/// <summary>
	/// Returns whether <paramref name="value"/> is almost zero based on <paramref name="epsilon"/>.
	/// </summary>
	[MethodImpl(Options)]
	public static bool AlmostZero(float value, float epsilon = Epsilon)
	{
		//Behavior identical to -epsilon < value && value < epsilon, including when value is float.NaN

		uint converted = BitConverter.SingleToUInt32Bits(value) << 1;
		uint threshold = BitConverter.SingleToUInt32Bits(epsilon) << 1;
		return converted < threshold;
	}
}