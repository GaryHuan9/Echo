using System;
using System.Threading;
using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Processes.Composition;

[EchoSourceUsable]
public record AutoExposure : ICompositeLayer
{
	/// <summary>
	/// The label of the layer to operate on.
	/// </summary>
	[EchoSourceUsable]
	public string TargetLayer { get; init; } = "main";

	/// <summary>
	/// Additional weight given to center pixels.
	/// </summary>
	/// <remarks>Zero means all pixels will be weighted equally.</remarks>
	[EchoSourceUsable]
	public float CenterWeight { get; init; } = 1f;

	/// <summary>
	/// The normalized (0 to 1) percent of darker pixels to discard when calculating the exposure.
	/// </summary>
	[EchoSourceUsable]
	public float PercentLowerBound { get; init; } = 0.55f;

	/// <summary>
	/// The normalized (0 to 1) percent of brighter pixels to discard when calculating the exposure.
	/// </summary>
	[EchoSourceUsable]
	public float PercentUpperBound { get; init; } = 0.03f;

	/// <summary>
	/// The luminance that the 'average' should align to.
	/// </summary>
	[EchoSourceUsable]
	public float AverageLuminance { get; init; } = 0.45f;

	/// <inheritdoc/>
	[EchoSourceUsable]
	public bool Enabled { get; init; } = true;

	const int BinCount = 128;

	public async ComputeTask ExecuteAsync(ICompositeContext context)
	{
		var sourceTexture = context.GetWriteTexture<RGB128>(TargetLayer);
		(float min, float max) = await GrabLuminanceRange(context, sourceTexture);

		float logMin = GetLog(min);
		float logMax = GetLog(max);
		float logStep = (logMax - logMin) / (BinCount - 1); //Amount of log luminance corresponding to each bin

		float[] bins = await MakeLuminanceHistogram(context, sourceTexture, logMin, logStep);
		float averageIndex = GetIndexAverage(bins, PercentLowerBound, 1f - PercentUpperBound);
		float average = GetExp(logMin + averageIndex * logStep);

		if (!FastMath.Positive(average)) return;
		float exposure = AverageLuminance / average;
		await context.RunAsync(MainPass, sourceTexture.size);

		void MainPass(Int2 position) => sourceTexture.Set(position, sourceTexture.Get(position) * exposure);
	}

	async ComputeTask<float[]> MakeLuminanceHistogram(ICompositeContext context, TextureGrid<RGB128> sourceTexture, float logMin, float logStep)
	{
		var bins = new float[BinCount];
		var locker = new SpinLock();
		float logStepR = 1f / logStep;
		if (FastMath.AlmostZero(logStep)) logStepR = 0f;

		await context.RunAsync(MainPass, sourceTexture.size.Y);
		return bins;

		void MainPass(uint y)
		{
			Span<float> rowBins = stackalloc float[BinCount];

			for (int x = 0; x < sourceTexture.size.X; x++)
			{
				Int2 position = new Int2(x, (int)y);

				float distance = Float2.Distance(sourceTexture.ToUV(position), Float2.Half) * 2f;
				float weight = (1f - Curves.EaseOutSmooth(distance)) * CenterWeight + 1f;
				float logLuminance = GetLog(sourceTexture.Get(position).Luminance);

				//Split the luminance into the two bins that are the closest
				float bin = (logLuminance - logMin) * logStepR;
				int bin0 = (int)bin;
				int bin1 = bin0 + 1;

				if (bin1 == rowBins.Length)
				{
					--bin0;
					--bin1;
				}

				rowBins[bin0] += (bin1 - bin) * weight;
				rowBins[bin1] += (bin - bin0) * weight;
			}

			bool lockTaken = false;

			try
			{
				locker.Enter(ref lockTaken);
				for (int i = 0; i < BinCount; i++) bins[i] += rowBins[i];
			}
			finally
			{
				if (lockTaken) locker.Exit();
			}
		}
	}

	static async ComputeTask<(float min, float max)> GrabLuminanceRange(ICompositeContext context, TextureGrid<RGB128> sourceBuffer)
	{
		var locker = new SpinLock();

		float min = float.PositiveInfinity;
		float max = float.NegativeInfinity;

		await context.RunAsync(MainPass, sourceBuffer.size.Y);
		return (min, max);

		void MainPass(uint y)
		{
			float rowMin = float.PositiveInfinity;
			float rowMax = float.NegativeInfinity;

			for (int x = 0; x < sourceBuffer.size.X; x++)
			{
				RGB128 value = sourceBuffer.Get(new Int2(x, (int)y));
				float luminance = value.Luminance;
				rowMin = FastMath.Min(rowMin, luminance);
				rowMax = FastMath.Max(rowMax, luminance);
			}

			bool lockTaken = false;

			try
			{
				locker.Enter(ref lockTaken);
				min = FastMath.Min(min, rowMin);
				max = FastMath.Max(max, rowMax);
			}
			finally
			{
				if (lockTaken) locker.Exit();
			}
		}
	}

	/// <summary>
	/// Returns the average index based on a list of weights.
	/// </summary>
	/// <param name="weights">The list of weights; an index containing a higher value means the average will lean more towards that index.</param>
	/// <param name="lowerBound">The normalized (0 to 1) threshold of when to begin considering the indices.</param>
	/// <param name="upperBound">The normalized (0 to 1) limit of when to stop considering the indices.</param>
	static float GetIndexAverage(ReadOnlySpan<float> weights, float lowerBound, float upperBound)
	{
		Ensure.IsTrue(lowerBound < upperBound);

		float totalWeight = 0f;
		foreach (float weight in weights) totalWeight += weight;

		float skipHead = lowerBound * totalWeight;
		float skipTail = upperBound * totalWeight;

		float current = 0f;
		float indexed = 0f;
		float included = 0f;

		for (int i = 0; i < weights.Length; i++)
		{
			//Slice the region based on the skip bounds
			float height = weights[i];
			float head = FastMath.Max(skipHead, current);
			float tail = FastMath.Min(skipTail, current + height);

			if (tail > head)
			{
				//Record if a portion of the current weight is not ignored
				float difference = tail - head;
				indexed += i * difference;
				included += difference;
			}

			current += height;
		}

		return indexed / included;
	}

	static float GetLog(float value) => MathF.Log(value + FastMath.Epsilon);
	static float GetExp(float value) => MathF.Exp(value) - FastMath.Epsilon;
}