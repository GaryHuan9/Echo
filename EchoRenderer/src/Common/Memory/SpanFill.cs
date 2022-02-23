using System;

namespace EchoRenderer.Common.Memory;

/// <summary>
/// A small struct that helps filling in a <see cref="Span{T}"/> easier.
/// </summary>
public ref struct SpanFill<T>
{
	public SpanFill(Span<T> span, int start = 0)
	{
		this.span = span;
		current = start - 1;
	}

	public readonly Span<T> span;

	int current;

	/// <summary>
	/// Returns the number of items that is currently filled in our target <see cref="Span{T}"/>.
	/// </summary>
	public int Count => current + 1;

	/// <summary>
	/// Returns whether the <see cref="Span{T}"/> that this <see cref="SpanFill{T}"/> is filling is full.
	/// </summary>
	public bool IsFull => Count == span.Length;

	/// <summary>
	/// Returns a new <see cref="Span{T}"/> that is only the slice of <see cref="span"/> that is already filled.
	/// </summary>
	public Span<T> Filled => span[..Count];

	/// <summary>
	/// Adds <paramref name="item"/> to <see cref="span"/>.
	/// </summary>
	public void Add(in T item) => span[++current] = item;

	public static implicit operator SpanFill<T>(in Span<T> span) => new(span);
	public static implicit operator Span<T>(in SpanFill<T> fill) => fill.span;
}
public static class SpanFillExtensions
{
	/// <summary>
	/// Creates a new <see cref="SpanFill{T}"/> over <paramref name="span"/> starting at <paramref name="start"/>.
	/// </summary>
	public static SpanFill<T> AsFill<T>(this Span<T> span, int start = 0) => new(span, start);
}