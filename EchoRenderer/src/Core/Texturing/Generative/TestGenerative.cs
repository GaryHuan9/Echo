using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics.Noises;
using CodeHelpers.Packed;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics.Primitives;

namespace EchoRenderer.Core.Texturing.Generative;

public class TestGenerative : CacheableTexture
{
	public TestGenerative(int seed, int layer, float persistence = 0.5f)
	{
		this.persistence = persistence;
		simplices = new Simplex[layer];

		float frequency = 1f;

		for (int i = 0; i < layer; i++)
		{
			simplices[i] = new Simplex(seed ^ i, 1024);

			inverseScale += frequency;
			frequency *= persistence;
		}

		inverseScale = 1f / inverseScale;
	}

	readonly float persistence;
	readonly float inverseScale;

	readonly Simplex[] simplices;

	protected override RGBA32 Sample(Float2 position)
	{
		// Float2 q = new Float2(FractionalBrownianMotion(position), FractionalBrownianMotion(position + new Float2(5.2f, 1.3f)));
		// Float2 r = new Float2(FractionalBrownianMotion(position + 4f * q + new Float2(1.7f, 9.2f)), FractionalBrownianMotion(position + 4f * q + new Float2(8.3f, 2.8f)));
		//
		// float v = FractionalBrownianMotion(position + 4f * r);

		float v0 = FractionalBrownianMotion(position);

		float v1 = FractionalBrownianMotion(position + new Float2(v0 * 5.2f, v0 * 2.3f));
		float v2 = FractionalBrownianMotion(position + new Float2(v1 * 2.7f, v1 * 4.7f));
		float v3 = FractionalBrownianMotion(position + new Float2(v2 * 3.1f, v2 * 1.4f));

		return new RGBA32(new Float3(v1, v2, v3));
	}

	float FractionalBrownianMotion(Float2 position)
	{
		float sum = 0f;
		float frequency = 1f;

		for (int i = 0; i < simplices.Length; i++)
		{
			sum += simplices[i].Sample(position / frequency) * frequency;
			frequency *= persistence;
		}

		return sum * inverseScale;
	}
}