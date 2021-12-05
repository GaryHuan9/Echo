using EchoRenderer.Mathematics;

namespace EchoRenderer.Rendering.Sampling
{
	public readonly struct Sample1
	{
		public Sample1(float u) => this.u = FastMath.ClampEpsilon(u);

		public readonly float u;
	}
}