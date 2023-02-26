using Echo.Core.Common.Diagnostics;

namespace Echo.Core.Common.Mathematics.Primitives;

/// <summary>
/// Represents something of type <typeparamref name="T"/> with a particular probability density function (pdf) value associated with it.
/// </summary>
public readonly struct Probable<T>
{
	public Probable(in T content, float pdf)
	{
		Ensure.IsTrue(FastMath.AlmostZero(pdf) || FastMath.Positive(pdf));

		this.content = content;
		this.pdf = pdf;
	}

	public readonly T content;
	public readonly float pdf;

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
	public static implicit operator Probable<T>(in (T content, float pdf) pair) => new(pair.content, pair.pdf);

	/// <summary>
	/// Converts a <see cref="Probable{T}"/> to its <see cref="content"/>.
	/// </summary>
	public static implicit operator T(in Probable<T> probable) => probable.content;

	public void Deconstruct(out T _content, out float _pdf)
	{
		_content = content;
		_pdf = pdf;
	}

	public override string ToString() => $"{nameof(content)}: {content}, {nameof(pdf)}: {pdf}";
}