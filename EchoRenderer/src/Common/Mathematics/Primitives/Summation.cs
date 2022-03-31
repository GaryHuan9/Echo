using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Packed;

namespace EchoRenderer.Common.Mathematics.Primitives;

/// <summary>
/// An accurate summation for <see cref="Float4"/> using the Kahan summation algorithm.
/// </summary>
public readonly struct Summation
{
	public Summation(in Float4 value)
	{
		total = value;
		error = Float4.Zero;
	}

	Summation(in Summation summation, in Float4 value)
	{
		Float4 delta = value - summation.error;
		total = summation.total + delta;
		error = total - summation.total - delta;
	}

	Summation(in Summation summation, in Summation value)
	{
		error = summation.error + value.error;
		Float4 delta = value.total - error;

		total = summation.total + delta;
		error = total - summation.total - delta;
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

	/// <summary>
	/// Adds one more <paramref name="value"/> to this <paramref name="summation"/>.
	/// </summary>
	public static Summation operator +(in Summation summation, in Float4 value) => new(summation, value);

	/// <inheritdoc cref="op_Addition(in Summation, in Float4"/>
	public static Summation operator +(in Summation summation, in Summation value) => new(summation, value);
}