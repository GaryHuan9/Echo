using System;
using System.Collections.Generic;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.Sampling
{
	public abstract class Sampler
	{
		protected Sampler(int sampleCount) => this.sampleCount = sampleCount;

		readonly List<int> lengthsSpan1 = new();
		readonly List<int> lengthsSpan2 = new();

		readonly List<Sample1[]> aggregateSpans1 = new();
		readonly List<Sample2[]> aggregateSpans2 = new();

		int indexSpan1;
		int indexSpan2;

		/// <summary>
		/// The maximum number of samples performed for one pixel.
		/// </summary>
		protected readonly int sampleCount;

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
		public virtual void RequestSpan1(int length)
		{
			var array = new Sample1[length * sampleCount];

			lengthsSpan1.Add(length);
			aggregateSpans1.Add(array);
		}

		/// <summary>
		/// Requests a span of two dimensional values with <paramref name="length"/> to be available.
		/// </summary>
		public virtual void RequestSpan2(int length)
		{
			var array = new Sample2[length * sampleCount];

			lengthsSpan2.Add(length);
			aggregateSpans2.Add(array);
		}

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

			indexSpan1 = -1;
			indexSpan2 = -1;
		}

		/// <summary>
		/// Returns the next one dimensional value of this sample.
		/// </summary>
		public abstract Sample1 Next1();

		/// <summary>
		/// Returns the next two dimensional value of this sample.
		/// </summary>
		public abstract Sample2 Next2();

		/// <summary>
		/// Returns the next span of one dimensional values of this sample.
		/// </summary>
		public abstract ReadOnlySpan<Sample1> NextSpan1();

		/// <summary>
		/// Returns the next span of two dimensional values of this sample.
		/// </summary>
		public abstract ReadOnlySpan<Sample2> NextSpan2();
	}
}