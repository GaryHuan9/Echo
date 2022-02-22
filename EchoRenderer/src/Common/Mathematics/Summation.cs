using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace EchoRenderer.Common.Mathematics;

/// <summary>
/// An accurate summation for <see cref="Vector128{T}"/> using the Kahan summation algorithm.
/// </summary>
public readonly struct Summation
{
	public Summation(in Vector128<float> value)
	{
		total = value;
		error = Vector128<float>.Zero;
	}

	Summation(in Summation summation, in Vector128<float> value)
	{
		var delta = Sse.Subtract(value, summation.error);
		total = Sse.Add(summation.total, delta);
		error = Sse.Subtract(Sse.Subtract(total, summation.total), delta);
	}

	readonly Vector128<float> total;
	readonly Vector128<float> error;

	/// <summary>
	/// Returns new <see cref="Summation"/> that is zero.
	/// </summary>
	public static Summation Zero => new(Vector128<float>.Zero);

	/// <summary>
	/// Returns the result of this <see cref="Summation"/>.
	/// </summary>
	public Vector128<float> Result => total;

	/// <summary>
	/// Adds one more <paramref name="value"/> to this <paramref name="summation"/>.
	/// </summary>
	public static Summation operator +(in Summation summation, in Vector128<float> value) => new(summation, value);
}