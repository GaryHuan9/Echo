using CodeHelpers.Mathematics;
using EchoRenderer.Rendering.PostProcessing.Operators;

namespace EchoRenderer.Rendering.PostProcessing.ToneMappers
{
	public class Reinhard : ToneMapper
	{
		/// <param name="engine"></param>
		/// <param name="whitePoint">The smallest exposure adjusted luminance that should be mapped to 1</param>
		public Reinhard(PostProcessingEngine engine, float whitePoint) : base(engine) => inverse = 1f / (whitePoint * whitePoint);

		readonly float inverse;
		float luminanceInverse;

		//http://www.cmap.polytechnique.fr/~peyre/cours/x2005signal/hdr_photographic.pdf
		//https://bruop.github.io/tonemapping/

		public override void Dispatch()
		{
			var grab = new LuminanceGrab(this, renderBuffer);

			grab.Run();

			if (grab.Luminance.AlmostEquals(0f)) return;
			luminanceInverse = 1f / (2f * grab.Luminance);

			base.Dispatch();
		}

		protected override float MapLuminance(float luminance)
		{
			luminance *= luminanceInverse;
			luminance *= (1f + luminance * inverse) / (1f + luminance);

			return luminance;
		}
	}
}