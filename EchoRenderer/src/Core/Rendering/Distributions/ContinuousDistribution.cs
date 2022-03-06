using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics.Randomization;

namespace EchoRenderer.Core.Rendering.Distributions;

/// <summary>
/// A continuous distribution that we can draw two different kinds of sample from: <see cref="Sample1D"/> and <see cref="Sample2D"/>.
/// We can also draw <see cref="Span{T}"/> of particular lengths of those two types of sample. Note that for different pixel samples
/// of the same pixel, the sequence of samples that we draw from this <see cref="ContinuousDistribution"/> should be identical.
/// NOTE: this class is not thread safe, create multiple instances for different threads using the copy constructor.
/// </summary>
public abstract class ContinuousDistribution
{
	/// <summary>
	/// Constructs a new <see cref="ContinuousDistribution"/> with <see cref="extend"/>.
	/// </summary>
	protected ContinuousDistribution(int extend)
	{
		this.extend = extend;

		arrayOnes = new List<SpanAggregate<Sample1D>>();
		arrayTwos = new List<SpanAggregate<Sample2D>>();
	}

	/// <summary>
	/// Constructs a copy of <paramref name="source"/>.
	/// NOTE: only information that specially defines this <see cref="ContinuousDistribution"/> needs to be cloned over.
	/// </summary>
	protected ContinuousDistribution(ContinuousDistribution source)
	{
		extend = source.extend;

		arrayOnes = Clone(source.arrayOnes);
		arrayTwos = Clone(source.arrayTwos);

		static List<SpanAggregate<T>> Clone<T>(IReadOnlyList<SpanAggregate<T>> list)
		{
			int count = list.Count;

			List<SpanAggregate<T>> clone = new List<SpanAggregate<T>>(count);
			for (int i = 0; i < count; i++) clone.Add(list[i].Replicate());

			return clone;
		}
	}

	/// <summary>
	/// The maximum number of pixel samples that will be performed for one pixel.
	/// </summary>
	public readonly int extend;

	protected readonly List<SpanAggregate<Sample1D>> arrayOnes;
	protected readonly List<SpanAggregate<Sample2D>> arrayTwos;

	int arrayOneIndex;
	int arrayTwoIndex;

	/// <summary>
	/// The position of the current processing pixel. This property is undefined if <see cref="BeginPixel"/> is never invoked.
	/// </summary>
	protected Int2 PixelPosition { get; private set; }

	/// <summary>
	/// The index (number) of the current processing pixel sample. After invoking <see cref="BeginSample"/>
	/// once, this property will always be between zero (inclusive) and <see cref="extend"/> (exclusive).
	/// </summary>
	protected int SampleNumber { get; private set; } = int.MinValue;

	/// <summary>
	/// The specific pseudo random number generator associated with this <see cref="ContinuousDistribution"/>. Note that
	/// this property is not cloned but rather simply set to null on the new instance when we use the copy constructor.
	/// </summary>
	public IRandom Random { get; set; }

	/// <summary>
	/// Requests a span of one dimensional values with <paramref name="length"/> to be available.
	/// </summary>
	public void RequestSpanOne(int length) => arrayOnes.Add(new SpanAggregate<Sample1D>(length, extend));

	/// <summary>
	/// Requests a span of two dimensional values with <paramref name="length"/> to be available.
	/// </summary>
	public void RequestSpanTwo(int length) => arrayTwos.Add(new SpanAggregate<Sample2D>(length, extend));

	/// <summary>
	/// Begins sampling on a new pixel at <paramref name="position"/>.
	/// </summary>
	public virtual void BeginPixel(Int2 position)
	{
		PixelPosition = position;
		SampleNumber = -1;
	}

	/// <summary>
	/// Begins a new pixel sample on the current pixel at <see cref="PixelPosition"/>. Must be invoked after <see cref="BeginPixel"/>.
	/// </summary>
	public virtual void BeginSample()
	{
		if (SampleNumber < -1) throw new Exception($"Operation invalid before {nameof(BeginPixel)} is invoked!");
		if (++SampleNumber >= extend) throw new Exception($"More than {extend} pixel samples has been requested!");

		arrayOneIndex = -1;
		arrayTwoIndex = -1;
	}

	/// <inheritdoc cref="Next1DCore"/>
	public abstract Sample1D Next1D();

	/// <inheritdoc cref="Next2DCore"/>
	public abstract Sample2D Next2D();

	/// <summary>
	/// Returns the next span of one dimensional values of this sample.
	/// </summary>
	public ReadOnlySpan<Sample1D> NextSpan1D() => ++arrayOneIndex < arrayOnes.Count ? arrayOnes[arrayOneIndex][SampleNumber] : Span<Sample1D>.Empty;

	/// <summary>
	/// Returns the next span of two dimensional values of this sample.
	/// </summary>
	public ReadOnlySpan<Sample2D> NextSpan2D() => ++arrayTwoIndex < arrayTwos.Count ? arrayTwos[arrayTwoIndex][SampleNumber] : Span<Sample2D>.Empty;


	public Sample1D Next1D([CallerFilePath] string filePath = default, [CallerLineNumber] int lineNumber = default)
	{

	}

	/// <summary>
	/// Draws and returns a new <see cref="Sample1D"/> from this <see cref="ContinuousDistribution"/>.
	/// </summary>
	protected abstract Sample1D Next1DCore();

	/// <summary>
	/// Draws and returns a new <see cref="Sample2D"/> from this <see cref="ContinuousDistribution"/>.
	/// </summary>
	protected abstract Sample2D Next2DCore();


	/// <summary>
	/// Returns the next span of one dimensional values of this sample.
	/// </summary>
	protected ReadOnlySpan<Sample1D> NextSpan1DCore() => ++arrayOneIndex < arrayOnes.Count ? arrayOnes[arrayOneIndex][SampleNumber] : Span<Sample1D>.Empty;

	/// <summary>
	/// Returns the next span of two dimensional values of this sample.
	/// </summary>
	protected ReadOnlySpan<Sample2D> NextSpan2DCore() => ++arrayTwoIndex < arrayTwos.Count ? arrayTwos[arrayTwoIndex][SampleNumber] : Span<Sample2D>.Empty;

	/// <summary>
	/// Produces and returns another copy of this <see cref="ContinuousDistribution"/> of the same <see cref="Object.GetType()"/> to be used for different threads.
	/// </summary>
	public abstract ContinuousDistribution Replicate();

	protected readonly struct SpanAggregate<T>
	{
		public SpanAggregate(int length, int sampleCount)
		{
			this.length = length;
			array = new T[length * sampleCount];
		}

		SpanAggregate(int length, T[] array)
		{
			this.length = length;
			this.array = array;
		}

		public readonly int length;
		public readonly T[] array;

		public Span<T> this[int sampleIndex] => array.AsSpan(sampleIndex * length, length);

		public SpanAggregate<T> Replicate() => new(length, new T[array.Length]);
	}
}