using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.Common.Mathematics.Randomization;

namespace Echo.Core.Evaluation.Distributions.Continuous;

using FP = CallerFilePathAttribute;
using LN = CallerLineNumberAttribute;

/// <summary>
/// A continuous distribution that we can draw two different kinds of sample from: <see cref="Sample1D"/> and <see cref="Sample2D"/>.
/// We can also draw <see cref="Span{T}"/> of particular lengths of those two types of sample. Note that for different pixel samples
/// of the same pixel, the sequence of samples that we draw from this <see cref="ContinuousDistribution"/> should be identical.
/// NOTE: this class is not thread safe, create multiple instances for different threads using the copy constructor.
/// </summary>
public abstract record ContinuousDistribution
{
	/// <summary>
	/// Constructs a new <see cref="ContinuousDistribution"/> with <see cref="Extend"/>.
	/// </summary>
	protected ContinuousDistribution() => Extend = 16;

	/// <summary>
	/// Copy constructs a <see cref="ContinuousDistribution"/> from <paramref name="source"/>.
	/// </summary>
	protected ContinuousDistribution(ContinuousDistribution source)
	{
		spans1D = new BufferDomain<Sample1D>();
		spans2D = new BufferDomain<Sample2D>();

		Extend = source.Extend;
		SampleNumber = int.MinValue;
	}

	readonly BufferDomain<Sample1D> spans1D = new();
	readonly BufferDomain<Sample2D> spans2D = new();

	MonoThread monoThread;

	readonly int _extend;

	/// <summary>
	/// The maximum number of pixel samples that will be performed for one pixel.
	/// </summary>
	public int Extend
	{
		get => _extend;
		init
		{
			if (value > 0) _extend = value;
			else throw ExceptionHelper.Invalid(nameof(value), value, InvalidType.outOfBounds);
		}
	}

	/// <summary>
	/// The position of the current processing pixel. This property is undefined if <see cref="BeginPixel"/> is never invoked.
	/// </summary>
	protected Int2 PixelPosition { get; private set; }

	/// <summary>
	/// The index (number) of the current processing pixel sample. After invoking <see cref="BeginSample"/>
	/// once, this property will always be between zero (inclusive) and <see cref="Extend"/> (exclusive).
	/// </summary>
	protected int SampleNumber { get; private set; } = int.MinValue;

	IRandom _prng;

	/// <summary>
	/// The specific pseudo random number generator associated with this <see cref="ContinuousDistribution"/>. Note that
	/// this property is not cloned but rather simply set to null on the new instance when we use the copy constructor.
	/// </summary>
	public IRandom Prng
	{
		get => _prng;
		set
		{
			if (value != null || _prng == null) _prng = value;
			else throw new Exception($"Once a {nameof(Prng)} is assigned, it cannot be removed to null.");
		}
	}

	/// <summary>
	/// Begins sampling on a new pixel at <paramref name="position"/>.
	/// </summary>
	public virtual void BeginPixel(Int2 position)
	{
		monoThread.Ensure();

		PixelPosition = position;
		SampleNumber = -1;
	}

	/// <summary>
	/// Begins a new pixel sample on the current pixel at <see cref="PixelPosition"/>. Must be invoked after <see cref="BeginPixel"/>.
	/// </summary>
	public virtual void BeginSample()
	{
		monoThread.Ensure();

		if (SampleNumber < -1) throw new Exception($"Operation invalid before {nameof(BeginPixel)} is invoked!");
		if (++SampleNumber >= Extend) throw new Exception($"More than {Extend} pixel samples has been requested!");

		spans1D.Reset(true);
		spans2D.Reset(true);
	}

#if DEBUG
	//
	/// <inheritdoc cref="Next1DCore"/>
	public Sample1D Next1D([FP] string filePath = default, [LN] int lineNumber = default)
	{
		EnsureIsSampling();
		return Next1DImpl();
	}

	/// <inheritdoc cref="Next2DCore"/>
	public Sample2D Next2D([FP] string filePath = default, [LN] int lineNumber = default)
	{
		EnsureIsSampling();
		return Next2DImpl();
	}

	/// <inheritdoc cref="NextSpan2DCore"/>
	public ReadOnlySpan<Sample1D> NextSpan1D(int length, [FP] string filePath = default, [LN] int lineNumber = default)
	{
		EnsureIsSampling();
		return NextSpan1DImpl(length);
	}

	/// <inheritdoc cref="NextSpan2DCore"/>
	public ReadOnlySpan<Sample2D> NextSpan2D(int length, [FP] string filePath = default, [LN] int lineNumber = default)
	{
		EnsureIsSampling();
		return NextSpan2DImpl(length);
	}

#else
	//
	/// <inheritdoc cref="Next1DImpl"/>
	public Sample1D Next1D() => Next1DImpl();

	/// <inheritdoc cref="Next2DImpl"/>
	public Sample2D Next2D() => Next2DImpl();

	/// <inheritdoc cref="NextSpan1DImpl"/>
	public ReadOnlySpan<Sample1D> NextSpan1D(int length) => NextSpan1DImpl(length);

	/// <inheritdoc cref="NextSpan2DImpl"/>
	public ReadOnlySpan<Sample2D> NextSpan2D(int length) => NextSpan2DImpl(length);

#endif

	/// <summary>
	/// Draws and returns a new <see cref="Sample1D"/> from this <see cref="ContinuousDistribution"/>.
	/// </summary>
	protected abstract Sample1D Next1DImpl();

	/// <summary>
	/// Draws and returns a new <see cref="Sample2D"/> from this <see cref="ContinuousDistribution"/>.
	/// </summary>
	protected abstract Sample2D Next2DImpl();

	/// <summary>
	/// Fills <paramref name="samples"/> with <see cref="Sample1D"/> values from this <see cref="ContinuousDistribution"/>.
	/// </summary>
	protected abstract void FillSpan1D(Span<Sample1D> samples);

	/// <summary>
	/// Fills <paramref name="samples"/> with <see cref="Sample2D"/> values from this <see cref="ContinuousDistribution"/>.
	/// </summary>
	protected abstract void FillSpan2D(Span<Sample2D> samples);

	/// <summary>
	/// Draws and returns a span with <paramref name="length"/> of <see cref="Sample1D"/> values from this <see cref="ContinuousDistribution"/>.
	/// </summary>
	ReadOnlySpan<Sample1D> NextSpan1DImpl(int length)
	{
		spans1D.TryFetch(length, out var span);

		FillSpan1D(span);
		return span;
	}

	/// <summary>
	/// Draws and returns a span with <paramref name="length"/> of <see cref="Sample2D"/> values from this <see cref="ContinuousDistribution"/>.
	/// </summary>
	ReadOnlySpan<Sample2D> NextSpan2DImpl(int length)
	{
		spans2D.TryFetch(length, out var span);

		FillSpan2D(span);
		return span;
	}

#if DEBUG
	//
	void EnsureIsSampling()
	{
		monoThread.Ensure();

		if (SampleNumber < 0) throw new Exception($"Operation invalid before {nameof(BeginSample)} is invoked!");
		if (SampleNumber >= extend) throw new Exception($"More than {extend} pixel samples has been requested!");
	}

#endif

	/// <summary>
	/// A collection of array buffers of <typeparamref name="T"/> with different <see cref="Array.Length"/>.
	/// New buffers and lengths are created and determined as we fetch. Begin reusing the buffers by invoking
	/// the <see cref="Reset"/> method.
	/// </summary>
	protected class BufferDomain<T>
	{
		readonly List<T[]> cache = new();

		int next = int.MinValue; //The next buffer to be immediately fetched
		int head = int.MinValue; //The top of current initialized buffers

		/// <summary>
		/// Retrieves the next buffer, which should always have <paramref name="length"/>.
		/// Returns whether the returned buffer is initialized as defined by <see cref="Reset"/>.
		/// </summary>
		public bool TryFetch(int length, out T[] buffer)
		{
			if (++next > head)
			{
				//Push head forward
				if (++head < cache.Count)
				{
					buffer = cache[head];
					Assert.AreEqual(buffer.Length, length);
				}
				else
				{
					buffer = GC.AllocateUninitializedArray<T>(length);
					cache.Add(buffer);
				}

				Assert.AreEqual(next, head);
				return true;
			}

			buffer = cache[next];
			Assert.AreEqual(buffer.Length, length);
			return false;
		}

		/// <summary>
		/// Resets to begin reusing cached buffers. If <paramref name="soft"/> is true, then the content of the cached buffers
		/// are not marked as uninitialized, otherwise if it is false, then all cached buffers are considered as uninitialized.
		/// </summary>
		public void Reset(bool soft)
		{
			next = -1;
			if (soft) return;
			head = -1;
		}
	}
}