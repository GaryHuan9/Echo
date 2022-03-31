namespace EchoRenderer.Common.Mathematics.Primitives;

/// <summary>
/// Represents something of type <typeparamref name="T"/> with a certain probability density function (pdf) value.
/// </summary>
public readonly struct Probable<T>
{
	public Probable(in T content, float pdf)
	{
		this.content = content;
		this.pdf = pdf;
	}

	public readonly T content;
	public readonly float pdf;

	public static Probable<T> Zero => default;

	public bool IsZero => !FastMath.Positive(pdf);

	public static implicit operator Probable<T>(in (T content, float pdf) pair) => new(pair.content, pair.pdf);

	public static implicit operator T(in Probable<T> probable) => probable.content;
}