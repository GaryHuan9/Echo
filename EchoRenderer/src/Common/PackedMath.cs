using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Common;

public static class PackedMath
{
	/// <summary>
	/// Linearly interpolates between <paramref name="left"/> and <paramref name="right"/> based on <paramref name="time"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<float> Lerp(in Vector128<float> left, in Vector128<float> right, in Vector128<float> time)
	{
		var length = Sse.Subtract(right, left);
		return FMA(length, time, left);
	}

	/// <summary>
	/// Numerically clamps <paramref name="value"/> between <paramref name="min"/> and <paramref name="max"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<float> Clamp(in Vector128<float> value, in Vector128<float> min, in Vector128<float> max) => Sse.Min(max, Sse.Max(min, value));

	/// <summary>
	/// Numerically clamps <paramref name="value"/> between zero and one.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<float> Clamp01(in Vector128<float> value) => Clamp(value, Vector128.Create(0f), Vector128.Create(1f));

	/// <summary>
	/// Fused multiply and add <paramref name="value"/> with <paramref name="multiplier"/> first and then <paramref name="adder"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<float> FMA(in Vector128<float> value, in Vector128<float> multiplier, in Vector128<float> adder)
	{
		if (Fma.IsSupported) return Fma.MultiplyAdd(value, multiplier, adder);
		return Sse.Add(Sse.Multiply(value, multiplier), adder);
	}

	/// <summary>
	/// Returns the approximated luminance of <paramref name="color"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GetLuminance(in Vector128<float> color)
	{
		if (Sse41.IsSupported) return Sse41.DotProduct(color, Constants.LuminanceVector, 0b0111_0001).GetElement(0);

		Vector128<float> result = Sse.Multiply(color, Constants.LuminanceVector);
		return result.GetElement(0) + result.GetElement(1) + result.GetElement(2);
	}

	/// <summary>
	/// Returns a color with minimal individual component sum that has <paramref name="luminance"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<float> CreateLuminance(float luminance) => Sse.Multiply(Constants.LuminanceVector, Vector128.Create(luminance));

	/// <summary>
	/// Returns whether all elements in <paramref name="value"/> is less than positive
	/// <paramref name="epsilon"/> and greater than negative <paramref name="epsilon"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool AlmostZero(in Vector128<float> value, float epsilon = Scalars.Epsilon)
	{
		Vector128<float> compare = Sse.Or
		(
			Sse.CompareLessThanOrEqual(value, Vector128.Create(-epsilon)),
			Sse.CompareGreaterThanOrEqual(value, Vector128.Create(epsilon))
		);

		return Sse.MoveMask(compare) == 0;
	}
}