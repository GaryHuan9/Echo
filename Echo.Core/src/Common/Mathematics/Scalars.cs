using System;

namespace Echo.Core.Common.Mathematics;

public static class Scalars
{
	/// <summary>
	/// A positive number that is really close to zero (1E-5 or 0.00001).
	/// </summary>
	public const float Epsilon = 1E-5f;

	/// <summary>
	/// The mathematical constant <see cref="Pi"/> (float32).
	/// </summary>
	public const float Pi = (float)SourcePi;

	/// <summary>
	/// The reciprocal of <see cref="Pi"/> (float32).
	/// </summary>
	public const float PiR = (float)(1m / SourcePi);

	/// <summary>
	/// The mathematical constant <see cref="Tau"/>, identical to 2 * Pi (float32).
	/// </summary>
	public const float Tau = (float)(SourcePi * 2m);

	/// <summary>
	/// The reciprocal of <see cref="Tau"/> (float32).
	/// </summary>
	public const float TauR = (float)(0.5m / SourcePi);

	/// <summary>
	/// Euler's number (float32).
	/// </summary>
	public const float E = (float)SourceE;

	/// <summary>
	/// The golden ratio (float32).
	/// </summary>
	public const float Phi = (float)SourcePhi;

	/// <summary>
	/// The square root of constant 2 (float32).
	/// </summary>
	public const float Root2 = (float)SourceRoot2;

	/// <summary>
	/// The square root of constant 3 (float32).
	/// </summary>
	public const float Root3 = (float)SourceRoot3;

	/// <summary>
	/// The mathematical constant <see cref="PiF64"/> (float64).
	/// </summary>
	public const double PiF64 = (double)SourcePi;

	/// <summary>
	/// The reciprocal of <see cref="PiF64"/> (float64).
	/// </summary>
	public const double PiRF64 = (double)(1m / SourcePi);

	/// <summary>
	/// The mathematical constant <see cref="TauF64"/>, identical to 2 * Pi (float64).
	/// </summary>
	public const double TauF64 = (double)(SourcePi * 2m);

	/// <summary>
	/// The reciprocal of <see cref="TauF64"/> (float64).
	/// </summary>
	public const double TauRF64 = (double)(0.5m / SourcePi);

	/// <summary>
	/// Euler's number (float64).
	/// </summary>
	public const double EF64 = (double)SourceE;

	/// <summary>
	/// The golden ratio (float64).
	/// </summary>
	public const double PhiF64 = (double)SourcePhi;

	/// <summary>
	/// The square root of constant 2 (float64).
	/// </summary>
	public const double Root2F64 = (double)SourceRoot2;

	/// <summary>
	/// The square root of constant 3 (float64).
	/// </summary>
	public const double Root3F64 = (double)SourceRoot3;

	const decimal SourcePi /*    */ = 3.1415926535897932384626433832795028841971693993751058209749445923078164062862m;
	const decimal SourceE /*     */ = 2.7182818284590452353602874713526624977572470936999595749669676277240766303535m;
	const decimal SourcePhi /*   */ = 1.6180339887498948482045868343656381177203091798057628621354486227052604628189m;
	const decimal SourceRoot2 /* */ = 1.4142135623730950488016887242096980785696718753769480731766797379907324784621m;
	const decimal SourceRoot3 /* */ = 1.7320508075688772935274463415058723669428052538103806280558069794519330169088m;

	public static float ToDegrees(float radians) => radians * (float)(180m / SourcePi);
	public static float ToRadians(float degrees) => degrees * (float)(SourcePi / 180m);

	public static double ToDegrees(double radians) => radians * (double)(180m / SourcePi);
	public static double ToRadians(double degrees) => degrees * (double)(SourcePi / 180m);

	public static float Lerp(float left, float right, float value) => (right - left) * value + left;
	public static int Lerp(int left, int right, int value) => (right - left) * value + left;

	// ReSharper disable once CompareOfFloatsByEqualityOperator
	public static float InverseLerp(float left, float right, float value) => left == right ? 0f : (value - left) / (right - left);
	public static int InverseLerp(int left, int right, int value) => left == right ? 0 : (value - left) / (right - left);

	public static float Clamp(this float value, float min = 0f, float max = 1f) => value < min ? min : value > max ? max : value;
	public static int Clamp(this int value, int min = 0, int max = 1) => value < min ? min : value > max ? max : value;
	public static float Clamp(this int value, float min, float max = 1f) => value < min ? min : value > max ? max : value;
	public static double Clamp(this double value, double min = 0d, double max = 1d) => value < min ? min : value > max ? max : value;

	/// <summary>
	/// Convert <paramref name="value"/> to an angle between -180f (Exclusive) and 180f (Inclusive) with the same rotational value as input.
	/// </summary>
	public static float ToSignedAngle(this float value) => -(180f - value).Repeat(360f) + 180f;

	/// <summary>
	/// Convert <paramref name="value"/> to an angle between -179 and 180 with the same rotational value as input.
	/// </summary>
	public static int ToSignedAngle(this int value) => -(180 - value).Repeat(360) + 180;

	/// <summary>
	/// Convert <paramref name="value"/> to an angle between 0f (Inclusive) and 360f (Exclusive) with the same rotational value as input.
	/// </summary>
	public static float ToUnsignedAngle(this float value) => value.Repeat(360f);

	/// <summary>
	/// Convert <paramref name="value"/> to an angle between 0 and 359 with the same rotational value as input.
	/// </summary>
	public static int ToUnsignedAngle(this int value) => value.Repeat(360);

	/// <summary>
	/// Converts <paramref name="value"/>, a number from zero to positive one. Into a range from negative one to positive one.
	/// </summary>
	public static float To1To1(this float value) => value * 2f - 1f;

	/// <summary>
	/// Converts <paramref name="value"/>, a number from negative one to positive one. Into a range from zero to positive one.
	/// </summary>
	public static float To0To1(this float value) => (value + 1f) / 2f;

	public static bool IsPowerOfTwo(this int value) => (value & -value) == value; //NOTE: This returns true for 0, which is not a power of two
	public static bool IsPowerOfTwo(this long value) => (value & -value) == value;

	/// <summary>
	/// Returns whether <paramref name="value"/> and <paramref name="other"/> are almost equal based on <paramref name="epsilon"/>.
	/// Uses relative comparison to compute the distance. This method is approximately four to five times slower than regular ==.
	/// </summary>
	public static bool AlmostEquals(this float value, float other = 0f, float epsilon = 1E-5f)
	{
		if (value == other) return true;                 //Handles absolute equals and degenerate cases
		const float Normal = (1L << 23) * float.Epsilon; //The smallest positive (non-zero) normal value that can be stored in a float

		float difference = Math.Abs(value - other);

		//If too close to zero to use relative comparison
		if (value == 0f || other == 0f || difference < Normal) return difference < epsilon * Normal;

		//Relative comparison
		float sum = Math.Abs(value) + Math.Abs(other);
		return difference < epsilon * Math.Min(sum, float.MaxValue);
	}

	/// <summary>
	/// Returns whether <paramref name="value"/> and <paramref name="other"/> are almost equal based on <paramref name="epsilon"/>.
	/// Uses relative comparison to compute the distance. This method is approximately four to five times slower than regular ==.
	/// </summary>
	public static bool AlmostEquals(this double value, double other = 0d, double epsilon = 1E-10d)
	{
		if (value == other) return true;                   //Handles absolute equals and degenerate cases
		const double Normal = (1L << 52) * double.Epsilon; //The smallest positive (non-zero) normal value that can be stored in a double

		double difference = Math.Abs(value - other);

		//If too close to zero to use relative comparison
		if (value == 0d || other == 0d || difference < Normal) return difference < epsilon * Normal;

		//Relative comparison
		double sum = Math.Abs(value) + Math.Abs(other);
		return difference < epsilon * Math.Min(sum, double.MaxValue);
	}

	public static int Sign(this float value) => AlmostEquals(value) ? 0 : Math.Sign(value);
	public static int Sign(this int value) => Math.Sign(value);

	/// <summary>
	/// Wraps <paramref name="value"/> between 0 (inclusive) and <paramref name="length"/> (exclusive).
	/// <paramref name="value"/> is repeated across the domain. Works for negative numbers.
	/// </summary>
	public static float Repeat(this float value, float length)
	{
		if ((0f <= value) & (value < length)) return value;
		float mod = value % length;
		return mod < 0f ? mod + length : mod;
	}

	/// <summary>
	/// Wraps <paramref name="value"/> between 0 (inclusive) and <paramref name="length"/> (exclusive).
	/// <paramref name="value"/> is repeated across the domain. Works for negative numbers.
	/// </summary>
	public static int Repeat(this int value, int length)
	{
		if ((0 <= value) & (value < length)) return value;
		int mod = value % length;
		return mod < 0 ? mod + length : mod;
	}

	/// <summary>
	/// Wraps <paramref name="value"/> between 0 (inclusive) and <paramref name="length"/> (exclusive).
	/// <paramref name="value"/> is repeated across the domain. Works for negative numbers.
	/// </summary>
	public static double Repeat(this double value, double length)
	{
		if ((0d <= value) & (value < length)) return value;
		double mod = value % length;
		return mod < 0d ? mod + length : mod;
	}

	/// <summary>
	/// Wraps <paramref name="value"/> between 0 (inclusive) and <paramref name="length"/> (exclusive).
	/// <paramref name="value"/> is repeated across the domain. Works for negative numbers.
	/// </summary>
	public static long Repeat(this long value, long length)
	{
		if ((0L <= value) & (value < length)) return value;
		long mod = value % length;
		return mod < 0L ? mod + length : mod;
	}

	/// <summary>
	/// Wraps <paramref name="value"/> between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// <paramref name="value"/> is repeated across the domain. Works for negative numbers.
	/// </summary>
	public static float Repeat(this float value, float min, float max) => (value - min).Repeat(max - min) + min;

	/// <summary>
	/// Wraps <paramref name="value"/> between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// <paramref name="value"/> is repeated across the domain. Works for negative numbers.
	/// </summary>
	public static int Repeat(this int value, int min, int max) => (value - min).Repeat(max - min) + min;

	/// <summary>
	/// Wraps <paramref name="value"/> between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// <paramref name="value"/> is repeated across the domain. Works for negative numbers.
	/// </summary>
	public static double Repeat(this double value, double min, double max) => (value - min).Repeat(max - min) + min;

	/// <summary>
	/// Wraps <paramref name="value"/> between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
	/// <paramref name="value"/> is repeated across the domain. Works for negative numbers.
	/// </summary>
	public static long Repeat(this long value, long min, long max) => (value - min).Repeat(max - min) + min;

	public static int Floor(this float value) => (int)Math.Floor(value);
	public static int Ceil(this float value) => (int)Math.Ceiling(value);
	public static int Round(this float value) => (int)Math.Round(value);

	public static int FlooredDivide(this int value, int divisor) => value / divisor - Convert.ToInt32((value < 0) ^ (divisor < 0) && value % divisor != 0);
	public static long FlooredDivide(this long value, long divisor) => value / divisor - Convert.ToInt64((value < 0) ^ (divisor < 0) && value % divisor != 0);

	public static int CeiledDivide(this int value, int divisor) => value / divisor + Convert.ToInt32((value < 0) ^ (divisor > 0) && value % divisor != 0);
	public static long CeiledDivide(this long value, long divisor) => value / divisor + Convert.ToInt64((value < 0) ^ (divisor > 0) && value % divisor != 0);

	public static float Damp(this float current, float target, ref float velocity, float smoothTime, float deltaTime)
	{
		//Implementation based on Game Programming Gems 4 Chapter 1.10
		smoothTime = Math.Max(smoothTime, Epsilon);

		float omega = 2f / smoothTime;  //The smooth coefficient
		float delta = current - target; //Change in position/value

		float exp = ApproximateExp(omega * deltaTime);
		float value = (velocity + omega * delta) * deltaTime;

		velocity = (velocity - omega * value) * exp;
		return target + (delta + value) * exp;

		//Uses Taylor Polynomials to approximate 1/exp; acceptable accuracy when domain: 0 < x < 1
		static float ApproximateExp(float value) => 1f / (1f + value + 0.48f * value * value + 0.235f * value * value * value);
	}
}