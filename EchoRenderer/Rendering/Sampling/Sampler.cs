using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.Sampling
{
	public abstract class Sampler
	{
		protected Sampler(long sampleCount) => this.sampleCount = sampleCount;

		/// <summary>
		/// The maximum number of samples performed for one pixel.
		/// </summary>
		protected readonly long sampleCount;

		/// <summary>
		/// The position of the current processing pixel.
		/// </summary>
		protected Int2 PixelPosition { get; private set; }

		/// <summary>
		/// The index of the current processing sample.
		/// </summary>
		protected long SampleIndex { get; private set; }

		/// <summary>
		/// Requests a span of one dimensional values with <paramref name="length"/> to be available.
		/// </summary>
		public abstract void RequestSpan1(int length);

		/// <summary>
		/// Requests a span of two dimensional values with <paramref name="length"/> to be available.
		/// </summary>
		public abstract void RequestSpan2(int length);

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