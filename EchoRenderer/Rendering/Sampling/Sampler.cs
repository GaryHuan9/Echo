using System;
using System.Collections.Generic;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.Sampling
{
	public abstract class Sampler
	{
		protected Sampler(int sampleCount) => this.sampleCount = sampleCount;

		protected readonly List<SpanAggregate<Sample1>> spanOnes = new();
		protected readonly List<SpanAggregate<Sample2>> spanTwos = new();

		/// <summary>
		/// The maximum number of samples performed for one pixel.
		/// </summary>
		protected readonly int sampleCount;

		int spanOneIndex;
		int spanTwoIndex;

		/// <summary>
		/// The position of the current processing pixel.
		/// </summary>
		protected Int2 PixelPosition { get; private set; }

		/// <summary>
		/// The index of the current processing sample.
		/// </summary>
		protected int SampleIndex { get; private set; }

		/// <summary>
		/// Requests a span of one dimensional values with <paramref name="length"/> to be available.
		/// </summary>
		public void RequestSpanOne(int length) => spanOnes.Add(new SpanAggregate<Sample1>(length, sampleCount));

		/// <summary>
		/// Requests a span of two dimensional values with <paramref name="length"/> to be available.
		/// </summary>
		public void RequestSpanTwo(int length) => spanTwos.Add(new SpanAggregate<Sample2>(length, sampleCount));

		/// <summary>
		/// Begins sampling on a new pixel at <paramref name="position"/>.
		/// </summary>
		public virtual void BeginPixel(Int2 position)
		{
			SampleIndex = -1;
			PixelPosition = position;
		}

		/// <summary>
		/// Begins a new sample on the current pixel.
		/// </summary>
		public virtual void BeginSample()
		{
			++SampleIndex;
			Assert.IsTrue(SampleIndex < sampleCount);

			spanOneIndex = -1;
			spanTwoIndex = -1;
		}

		/// <summary>
		/// Returns the next one dimensional value of this sample.
		/// </summary>
		public abstract Sample1 NextOne();

		/// <summary>
		/// Returns the next two dimensional value of this sample.
		/// </summary>
		public abstract Sample2 NextTwo();

		/// <summary>
		/// Returns the next span of one dimensional values of this sample.
		/// </summary>
		public ReadOnlySpan<Sample1> NextSpanOne() => spanOnes[++spanOneIndex][SampleIndex];

		/// <summary>
		/// Returns the next span of two dimensional values of this sample.
		/// </summary>
		public ReadOnlySpan<Sample2> NextSpanTwo() => spanTwos[++spanTwoIndex][SampleIndex];

		protected readonly struct SpanAggregate<T> where T : struct
		{
			public SpanAggregate(int length, int sampleCount)
			{
				this.length = length;
				array = new T[length * sampleCount];
			}

			public readonly int length;
			public readonly T[] array;

			public Span<T> this[int sampleIndex] => array.AsSpan(sampleIndex * length, length);
		}
	}
}