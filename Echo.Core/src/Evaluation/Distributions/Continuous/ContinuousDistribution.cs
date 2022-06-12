using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.Core.Common.Mathematics.Randomization;

namespace Echo.Core.Evaluation.Distributions.Continuous;

using FP = CallerFilePathAttribute;
using LN = CallerLineNumberAttribute;

/// <summary>
/// A continuous distribution that we can draw two different kinds of sample from: <see cref="Sample1D"/> and <see cref="Sample2D"/>.
/// We can also draw <see cref="Span{T}"/> of particular lengths of those two types of sample. Note that for different pixel samples
/// of the same pixel, the sequence of samples that we draw from this <see cref="ContinuousDistribution"/> should be identical.
/// NOTE: this class is not thread safe, create multiple instances for different threads using nondestructive mutation.
/// </summary>
public abstract record ContinuousDistribution
{
	/// <summary>
	/// Constructs a new <see cref="ContinuousDistribution"/> with <paramref name="extend"/>.
	/// </summary>
	protected ContinuousDistribution(int extend = 16) => Extend = extend;

	/// <summary>
	/// Copy constructs a <see cref="ContinuousDistribution"/> from <paramref name="source"/>.
	/// </summary>
	protected ContinuousDistribution(ContinuousDistribution source)
	{
		spans1D = new BufferDomain<Sample1D>();
		spans2D = new BufferDomain<Sample2D>();

		Extend = source.Extend;
		Prng = source.Prng with { };
		SessionNumber = int.MinValue;
	}

	readonly BufferDomain<Sample1D> spans1D = new();
	readonly BufferDomain<Sample2D> spans2D = new();

	MonoThread monoThread;

	readonly int _extend;
	readonly NotNull<Prng> _prng = new SquirrelPrng();

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
	/// The specific <see cref="Prng"/> associated with this <see cref="ContinuousDistribution"/>.
	/// </summary>
	public Prng Prng
	{
		get => _prng;
		init => _prng = value;
	}

	/// <summary>
	/// The position of the series of sampling session. The value is never negative.
	/// </summary>
	protected Int2 SeriesPosition { get; private set; }

	/// <summary>
	/// The index of the current session that is being sampled. After invoking <see cref="BeginSession"/>
	/// once, this property will always be between zero (inclusive) and <see cref="Extend"/> (exclusive).
	/// </summary>
	protected int SessionNumber { get; private set; } = int.MinValue;

	/// <summary>
	/// Begins a new series of sampling sessions.
	/// </summary>
	/// <param name="position">The </param>
	public virtual void BeginSeries(Int2 position)
	{
		monoThread.Ensure();
		Assert.IsTrue(position >= Int2.Zero);

		SeriesPosition = position;
		SessionNumber = -1;
	}

	/// <summary>
	/// Begins a sampling session in the current series. Must be invoked after <see cref="BeginSeries"/>.
	/// </summary>
	public virtual void BeginSession()
	{
		monoThread.Ensure();

		if (SessionNumber < -1) throw new Exception($"Operation invalid before {nameof(BeginSeries)} is invoked!");
		if (++SessionNumber >= Extend) throw new Exception($"More than {Extend} sessions requested in this series!");

		spans1D.Reset(true);
		spans2D.Reset(true);
	}

#if DEBUG
	//
	/// <inheritdoc cref="Next1DImpl"/>
	public Sample1D Next1D([FP] string filePath = default, [LN] int lineNumber = default)
	{
		EnsureIsSampling();
		return Next1DImpl();
	}

	/// <inheritdoc cref="Next2DImpl"/>
	public Sample2D Next2D([FP] string filePath = default, [LN] int lineNumber = default)
	{
		EnsureIsSampling();
		return Next2DImpl();
	}

	/// <inheritdoc cref="NextSpan2DImpl"/>
	public ReadOnlySpan<Sample1D> NextSpan1D(int length, [FP] string filePath = default, [LN] int lineNumber = default)
	{
		EnsureIsSampling();
		return NextSpan1DImpl(length);
	}

	/// <inheritdoc cref="NextSpan2DImpl"/>
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

	public virtual bool Equals(ContinuousDistribution other) => other?.GetType() == GetType() && other.Extend == Extend && other.Prng == Prng;
	public override int GetHashCode() => HashCode.Combine(GetType(), Extend, Prng);

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

	void EnsureIsSampling()
	{
		monoThread.Ensure();

		if (SessionNumber < 0) throw new Exception($"Operation invalid before {nameof(BeginSession)} is invoked!");
		if (SessionNumber >= Extend) throw new Exception($"More than {Extend} sessions requested in this series!");
	}

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