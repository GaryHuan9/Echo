using Echo.Core.Common.Diagnostics;

namespace Echo.Core.Common.Mathematics.Primitives;

/// <summary>
/// Represents something of type <typeparamref name="T"/> with a particular probability density function (pdf) value associated with it.
/// </summary>
public readonly record struct Probable<T>(in T content, float pdf)
{
	public readonly T content = content;
	public readonly float pdf = CheckPdf(pdf);

	/// <summary>
	/// Returns a <see cref="Probable{T}"/> with a <see cref="pdf"/> of 0.
	/// </summary>
	public static Probable<T> Impossible => default;

	/// <summary>
	/// Returns whether this <see cref="Probable{T}"/> is not possible (impossible).
	/// </summary>
	public bool NotPossible => !FastMath.Positive(pdf);

	/// <summary>
	/// Converts a tuple with (<see cref="content"/>, <see cref="pdf"/>) to a matching <see cref="Probable{T}"/>.
	/// </summary>
	public static implicit operator Probable<T>(in (T content, float pdf) tuple) => new(tuple.content, tuple.pdf);

	/// <summary>
	/// Converts a <see cref="Probable{T}"/> to its <see cref="content"/>.
	/// </summary>
	public static implicit operator T(in Probable<T> probable) => probable.content;

	static float CheckPdf(float pdf)
	{
		Ensure.IsTrue(pdf >= 0f);
		Ensure.IsTrue(float.IsFinite(pdf));
		return pdf;
	}
}