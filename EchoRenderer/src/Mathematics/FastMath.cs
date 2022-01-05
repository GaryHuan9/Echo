using System;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Mathematics
{
	public static class FastMath
	{
		static readonly float oneMinusEpsilon = Scalars.UInt32ToSingleBits(Scalars.SingleToUInt32Bits(1f) - 1u);

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
		/// Returns either sine or cosine using the Pythagoras identity sin^2 + cos^2 = 1
		/// NOTE: if <paramref name="value"/> is <see cref="float.NaN"/>, it is simply returned.
		/// </summary>
		public static float Identity(float value) => Sqrt0(MathF.FusedMultiplyAdd(value, -value, 1f));
	}
}