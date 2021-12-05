using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.Sampling
{
	public class StratifiedSampler : LimitedSampler
	{
		public StratifiedSampler(Int2 sampleSize, int dimensionCount) : base(sampleSize.Product, dimensionCount) => this.sampleSize = sampleSize;

		StratifiedSampler(StratifiedSampler sampler) : base(sampler)
		{
			sampleSize = sampler.sampleSize;
			Jitter = sampler.Jitter;
		}

		public readonly Int2 sampleSize;

		/// <summary>
		/// Returns whether the stratified samples are randomly shifted inside their individual cells.
		/// </summary>
		public bool Jitter { get; set; } = true;

		public override void BeginPixel(Int2 position)
		{
			base.BeginPixel(position);
			Assert.IsNotNull(PRNG);

			//Fill single samples
			foreach (SpanAggregate<Sample1> aggregate in ones)
			{
				FillIntervals(aggregate.array);
				PRNG.Shuffle<Sample1>(aggregate.array);
			}

			foreach (SpanAggregate<Sample2> aggregate in twos)
			{
				FillIntervals(aggregate.array, sampleSize);
				PRNG.Shuffle<Sample2>(aggregate.array);
			}

			//Fill span samples
			foreach (var aggregate in spanOnes)
			{
				for (int i = 0; i < aggregate.length; i++)
				{
					Span<Sample1> span = aggregate[i];

					FillIntervals(span);
					PRNG.Shuffle(span);
				}
			}

			foreach (var aggregate in spanTwos)
			{
				for (int i = 0; i < aggregate.length; i++)
				{
					LatinHypercube(aggregate[i]);
				}
			}
		}

		void FillIntervals(Span<Sample1> span)
		{
			float scale = 1f / span.Length;

			for (int i = 0; i < span.Length; i++)
			{
				ref Sample1 sample = ref span[i];
				float offset = Jitter ? PRNG.Next1() : 0.5f;
				sample = new Sample1((i + offset) * scale);
			}
		}

		void FillIntervals(Span<Sample2> span, Int2 size)
		{
			Assert.AreEqual(span.Length, size.Product);
			Float2 scale = 1f / size;

			Int2 position = Int2.zero;

			for (int i = 0; i < span.Length; i++)
			{
				ref Sample2 sample = ref span[i];

				Float2 offset = Jitter ? PRNG.Next2() : Float2.half;
				sample = new Sample2((position + offset) * scale);

				if (position.x < size.x - 1) position += Int2.right;
				else position = new Int2(0, position.y + 1);
			}
		}

		void LatinHypercube(Span<Sample2> span)
		{
			int length = span.Length;
			float scale = 1f / length;

			Span<float> spanX = stackalloc float[length];
			Span<float> spanY = stackalloc float[length];

			for (int i = 0; i < length; i++) spanX[i] = i;
			for (int i = 0; i < length; i++) spanY[i] = i;

			PRNG.Shuffle(spanX);
			PRNG.Shuffle(spanY);

			for (int i = 0; i < span.Length; i++)
			{
				ref Sample2 sample = ref span[i];
				Float2 offset = Jitter ? PRNG.Next2() : Float2.half;
				Float2 position = new Float2(spanX[i], spanY[i]);
				sample = new Sample2((position + offset) * scale);
			}
		}

		public override Sampler Replicate() => new StratifiedSampler(this);
	}
}