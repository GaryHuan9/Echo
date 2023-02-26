using System;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Textures.Generative;

/// <summary>
/// A little experimental cloud noise texture I made for fun.
/// </summary>
public class CloudTexture : CacheableTexture
{
	public CloudTexture(int seed, int layer, float persistence = 0.5f)
	{
		frequencies = new float[layer];
		frequenciesR = new float[layer];
		simplices = new Simplex[layer];

		float frequency = 1f;
		float scale = 0f;

		var random = new SystemPrng((uint)seed);

		for (int i = 0; i < layer; i++)
		{
			simplices[i] = new Simplex(random.NextUInt32(), 1024);

			frequencies[i] = frequency;
			frequenciesR[i] = 1f / frequency;
			scale += frequency;

			frequency *= persistence;
		}

		scaleR = 1f / scale;
	}

	static CloudTexture()
	{
		colorGrid0.Set(new Int2(0, 0), new RGB128(0.8f, 0.8f, 1.0f));
		colorGrid0.Set(new Int2(1, 0), new RGB128(0.9f, 0.8f, 0.9f));
		colorGrid0.Set(new Int2(0, 1), new RGB128(0.6f, 0.9f, 0.9f));
		colorGrid0.Set(new Int2(1, 1), new RGB128(0.9f, 1.0f, 1.0f));

		colorGrid1.Set(new Int2(0, 0), new RGB128(0.1f, 0.1f, 0.2f));
		colorGrid1.Set(new Int2(1, 0), new RGB128(0.1f, 0.1f, 0.3f));
		colorGrid1.Set(new Int2(0, 1), new RGB128(0.1f, 0.1f, 0.4f));
		colorGrid1.Set(new Int2(1, 1), new RGB128(0.1f, 0.2f, 0.2f));
	}

	readonly float[] frequencies;
	readonly float[] frequenciesR;
	readonly float scaleR;

	readonly Simplex[] simplices;

	static readonly ArrayGrid<RGB128> colorGrid0 = new(new Int2(2, 2));
	static readonly ArrayGrid<RGB128> colorGrid1 = new(new Int2(2, 2));

	protected override RGBA128 Sample(Float2 position)
	{
		Float2 p0 = new Float2
		(
			FractionalBrownianMotion(position + new Float2(8.8f, 3.8f)),
			FractionalBrownianMotion(position + new Float2(4.6f, 7.9f))
		);

		Float2 p1 = new Float2
		(
			FractionalBrownianMotion(position + p0 * 4f + new Float2(3.3f, 9.5f)),
			FractionalBrownianMotion(position + p0 * 4f + new Float2(1.5f, 0.2f))
		);

		float f = FractionalBrownianMotion(position + p1 * 4f);

		Float4 color0 = colorGrid0[p0];
		Float4 color1 = colorGrid1[p1];
		return (RGBA128)Float4.Lerp(color0, color1, MathF.Max(0f, f * f * f * 2f));
	}

	float FractionalBrownianMotion(Float2 position)
	{
		float sum = 0f;

		for (int i = 0; i < simplices.Length; i++)
		{
			sum += simplices[i].Sample(position * frequenciesR[i]) * frequencies[i];
		}

		return sum * scaleR;
	}
}