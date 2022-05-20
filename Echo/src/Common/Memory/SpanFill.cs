using System;
using CodeHelpers.Diagnostics;

namespace Echo.Common.Memory;

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

	readonly Span<T> span;

	int current;

	/// <summary>
	/// The number of items that is currently filled in our target <see cref="Span{T}"/>.
	/// </summary>
	public readonly int Count => current + 1;

	/// <summary>
	/// The maximum number of items that can be filled; also the maximum value of <see cref="Count"/>.
	/// </summary>
	public readonly int Length => span.Length;

	/// <summary>
	/// Whether this <see cref="SpanFill{T}"/> is completely empty.
	/// </summary>
	/// <remarks>That is, <see cref="Count"/> equals zero.</remarks>
	public readonly bool IsEmpty => Count == 0;

	/// <summary>
	/// Whether the <see cref="Span{T}"/> that this <see cref="SpanFill{T}"/> is filling is full.
	/// </summary>
	/// <remarks>That is, <see cref="Count"/> equals <see cref="Length"/>.</remarks>
	public readonly bool IsFull => Count == Length;

	/// <summary>
	/// Returns a new <see cref="Span{T}"/> that is only the slice of <see cref="span"/> that is already filled.
	/// </summary>
	public readonly Span<T> Filled => span[..Count];

	/// <summary>
	/// Adds <paramref name="item"/> to <see cref="span"/>.
	/// </summary>
	public void Add(in T item) => span[++current] = item;

	/// <summary>
	/// Throws an <see cref="InvalidOperationException"/> if <see cref="IsEmpty"/> is false.
	/// </summary>
	public readonly void ThrowIfNotEmpty()
	{
		if (IsEmpty) return;
		throw new InvalidOperationException();
	}

	/// <summary>
	/// Throws an <see cref="InvalidOperationException"/> if <see cref="Length"/> is too small.
	/// </summary>
	/// <param name="threshold">Throws if <see cref="Length"/> is smaller than this value.</param>
	public readonly void ThrowIfTooSmall(int threshold)
	{
		if (Length >= threshold) return;
		throw new InvalidOperationException();
	}
}

public static class SpanFillExtensions
{
	/// <summary>
	/// Creates a new <see cref="SpanFill{T}"/> over <paramref name="span"/> starting at <paramref name="start"/>.
	/// </summary>
	public static SpanFill<T> AsFill<T>(this Span<T> span, int start = 0) => new(span, start);

	/// <summary>
	/// Creates a new <see cref="SpanFill{T}"/> over <paramref name="view"/> starting at <paramref name="start"/>.
	/// </summary>
	public static SpanFill<T> AsFill<T>(this View<T> view, int start = 0) => new(view, start);
}