using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;

namespace Echo.Core.Evaluation.Distributions.Continuous;

/// <summary>
/// A stratified <see cref="ContinuousDistribution"/> that partitions the domain to improve the spread of the values drawn.
/// </summary>
public class StratifiedDistribution : HorizontalDistribution
{
	public StratifiedDistribution(int extend) : base(extend) { }

	public StratifiedDistribution(StratifiedDistribution source) : base(source) => Jitter = source.Jitter;

	/// <summary>
	/// Returns whether the stratified samples are randomly shifted inside their individual cells.
	/// </summary>
	public bool Jitter { get; set; } = true;

	public override void BeginPixel(Int2 position)
	{
		base.BeginPixel(position);
		Assert.IsNotNull(Prng);
	}

	public override ContinuousDistribution Replicate() => new StratifiedDistribution(this);

	protected override void FillSpan1D(Span<Sample1D> samples)
	{
		FillStratum(samples);
		Prng.Shuffle(samples);
	}

	protected override void FillSpan2D(Span<Sample2D> samples)
	{
		int roots = (int)MathF.Sqrt(samples.Length);

		if (roots * roots == samples.Length)
		{
			FillStratum(samples, roots);
			Prng.Shuffle(samples);
		}
		else LatinHypercube(samples);
	}

	void FillStratum(Span<Sample1D> span)
	{
		float lengthR = 1f / span.Length;

		for (int i = 0; i < span.Length; i++)
		{
			float offset = Jitter ? Prng.Next1() : 0.5f;
			span[i] = (Sample1D)((i + offset) * lengthR);
		}
	}

	void FillStratum(Span<Sample2D> span, int size)
	{
		Assert.AreEqual(span.Length, size * size);

		float sizeR = 1f / size;

		for (int x = 0; x < size; x++)
		for (int y = 0; y < size; y++)
		{
			Float2 offset = Jitter ? Prng.Next2() : Float2.Half;
			Float2 position = new Float2(x, y) + offset;
			span[x * size + y] = (Sample2D)(position * sizeR);
		}
	}

	[SkipLocalsInit]
	void LatinHypercube(Span<Sample2D> span)
	{
		int length = span.Length;
		float lengthR = 1f / length;

		Span<float> spanX = stackalloc float[length];
		Span<float> spanY = stackalloc float[length];

		for (int i = 0; i < length; i++) spanX[i] = i;
		for (int i = 0; i < length; i++) spanY[i] = i;

		Prng.Shuffle(spanX);
		Prng.Shuffle(spanY);

		for (int i = 0; i < span.Length; i++)
		{
			Float2 offset = Jitter ? Prng.Next2() : Float2.Half;
			Float2 position = new Float2(spanX[i], spanY[i]);
			span[i] = (Sample2D)((position + offset) * lengthR);
		}
	}
}