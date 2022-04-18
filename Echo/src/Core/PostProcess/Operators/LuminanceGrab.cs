using System;
using System.Runtime.CompilerServices;
using System.Threading;
using CodeHelpers.Packed;
using Echo.Common.Mathematics.Primitives;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grid;

namespace Echo.Core.PostProcess.Operators;

public sealed class LuminanceGrab : IDisposable
{
	public LuminanceGrab(PostProcessingWorker worker, TextureGrid<RGB128> sourceBuffer)
	{
		this.worker = worker;
		this.sourceBuffer = sourceBuffer;
	}

	public float Luminance { get; private set; }

	readonly PostProcessingWorker worker;
	readonly TextureGrid<RGB128> sourceBuffer;

	ThreadLocal<StrongBox<Summation>> sums = new(() => new StrongBox<Summation>(Summation.Zero), true);

	public void Run()
	{
		worker.RunPass(LuminancePass, sourceBuffer);

		var sum = Summation.Zero;

		foreach (StrongBox<Summation> box in sums.Values) sum += box.Value;
		Luminance = ((RGB128)sum.Result).Luminance / sourceBuffer.size.Product;
	}

	public void Dispose()
	{
		sums?.Dispose();
		sums = null;
	}

	void LuminancePass(Int2 position)
	{
		StrongBox<Summation> box = sums.Value;
		box.Value += sourceBuffer[position];
	}
}