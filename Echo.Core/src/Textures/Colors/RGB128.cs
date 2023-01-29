using System;
using System.Runtime.CompilerServices;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;

namespace Echo.Core.Textures.Colors;

/// <summary>
/// A color type with red, green, and blue channels, occupying a total of 16 bytes, or 128 bits.
/// The value for these three channels are always larger than or equals to zero, or less than
/// <see cref="float.PositiveInfinity"/>, and is never <see cref="float.NaN"/>.
/// </summary>
public readonly struct RGB128 : IColor<RGB128>, IFormattable
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public RGB128(float r, float g, float b) : this(Check(new Float4(r, g, b, 0f))) { }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public RGB128(float value) : this(Check(((Float4)value).XYZ_)) { }

	/// <summary>
	/// Create an <see cref="RGB128"/> without any checks.
	/// </summary>
	RGB128(in Float4 value) => d = value;

	readonly Float4 d; //The W component should always be zero

	const float EpsilonWeight = FastMath.Epsilon;
	const float RadianceWeightR = 0.212671f;
	const float RadianceWeightG = 0.715160f;
	const float RadianceWeightB = 0.072169f;

	public float R => d.X;
	public float G => d.Y;
	public float B => d.Z;

	public float Luminance => d.Dot(new Float4(RadianceWeightR, RadianceWeightG, RadianceWeightB, 0f));

	public bool IsZero
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => d < new Float4
		(
			EpsilonWeight / RadianceWeightR,
			EpsilonWeight / RadianceWeightG,
			EpsilonWeight / RadianceWeightB,
			float.MaxValue
		);
	}

	public static RGB128 Black => new(new Float4(0f, 0f, 0f, 0f)); //We can replace this with `new RGB128()` for smaller JIT asm, however if more collapsing is added to the JIT then this is probably fine
	public static RGB128 White => new(new Float4(1f, 1f, 1f, 0f));

	public override int GetHashCode() => d.GetHashCode();
	public override string ToString() => ToString(default);

	/// <inheritdoc/>
	public string ToString(string format, IFormatProvider provider = null) => d.XYZ.ToString(format, provider);

	/// <inheritdoc/>
	public RGBA128 ToRGBA128() => (RGBA128)this;

	/// <inheritdoc/>
	public RGB128 FromRGBA128(in RGBA128 value) => (RGB128)value;

	/// <summary>
	/// Increases <paramref name="value"/> slightly if needed so it is not zero.
	/// </summary>
	/// <remarks><see cref="IsZero"/> will be false on the returned value.</remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static RGB128 MaxEpsilon(in RGB128 value) => new(value.d.Max(new Float4
	(
		EpsilonWeight / RadianceWeightR,
		EpsilonWeight / RadianceWeightG,
		EpsilonWeight / RadianceWeightB,
		0f
	)));

	public static implicit operator Float4(in RGB128 value) => value.d;
	public static explicit operator RGB128(in Float4 value) => new(Check(value.XYZ_));
	public static explicit operator RGB128(in RGBA128 value) => new(((Float4)value).XYZ_);

	public static RGB128 operator +(in RGB128 first, in RGB128 second) => new(first.d + second.d);
	public static RGB128 operator *(in RGB128 first, in RGB128 second) => new(first.d * second.d);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static RGB128 operator *(in RGB128 first, float second)
	{
		Ensure.IsTrue(second >= 0f);
		Ensure.IsTrue(float.IsFinite(second));
		return new RGB128(first.d * second);
	}

	public static RGB128 operator -(in RGB128 first, in RGB128 second) => new(Check(first.d - second.d));

	public static RGB128 operator /(in RGB128 first, float second) => new(Check(first.d / second));
	public static RGB128 operator /(in RGB128 first, in RGB128 second) => Divide(first.d, second.d);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static RGB128 Divide(in Float4 first, in Float4 second)
	{
		Float4 result = first / (second + Float4.Ana);
		return new RGB128(Check(result));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static Float4 Check(in Float4 value)
	{
		Ensure.AreEqual(value.W, 0f);
		Ensure.IsTrue(value.XYZ >= Float3.Zero);
		Ensure.IsTrue(float.IsFinite(value.Sum));

		return value;
	}
}