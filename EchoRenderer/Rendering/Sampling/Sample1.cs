using CodeHelpers.Diagnostics;

namespace EchoRenderer.Rendering.Sampling
{
	public readonly struct Sample1
	{
		public Sample1(float u)
		{
			Assert.IsTrue(0f <= u);
			Assert.IsTrue(1f > u);
			this.u = u;
		}

		public readonly float u;
	}
}