using CodeHelpers.Packed;

namespace EchoRenderer.Common.Mathematics.Primitives;

/// <summary>
/// An accurate summation for <see cref="Float4"/> using the Kahan summation algorithm.
/// </summary>
public readonly struct Summation
{
	public Summation(in Float4 value) : this(value, Float4.Zero) { }

	Summation(in Float4 total, in Float4 error)
	{
		this.total = total;
		this.error = error;
	}

	readonly Float4 total;
	readonly Float4 error;

	/// <summary>
	/// Returns new <see cref="Summation"/> that is zero.
	/// </summary>
	public static Summation Zero => new(Float4.Zero);

	/// <summary>
	/// Returns the result of this <see cref="Summation"/>.
	/// </summary>
	public Float4 Result => total;

	public static Summation operator -(in Summation summation, in Float4 value) => summation + -value;
	public static Summation operator /(in Summation summation, in Float4 value) => summation * (1f / value);

	public static Summation operator +(in Summation summation, in Float4 value)
	{
		Float4 delta = value - summation.error;
		Float4 total = summation.total + delta;
		Float4 error = total - summation.total - delta;

		return new Summation(total, error);
	}

	public static Summation operator *(in Summation summation, in Float4 value) => new(summation.total * value, summation.error * value);

	public static Summation operator +(in Summation summation, in Summation value)
	{
		Float4 error = summation.error + value.error;
		Float4 delta = value.total - error;

		Float4 total = summation.total + delta;
		error = total - summation.total - delta;

		return new Summation(total, error);
	}

	public static Summation operator -(in Summation summation, in Summation value) => summation + new Summation(-value.total, -value.error);
}