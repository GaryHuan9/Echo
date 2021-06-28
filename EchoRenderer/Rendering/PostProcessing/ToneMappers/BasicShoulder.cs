using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.PostProcessing.ToneMappers
{
	public class BasicShoulder : ToneMapper
	{
		public BasicShoulder(PostProcessingEngine engine) : base(engine) { }

		public float Smoothness { get; set; } = 0.5f;

		//https://www.desmos.com/calculator/nngw01x7om

		protected override float MapLuminance(float luminance)
		{
			float oneLess = luminance - 1f;

			float h = (0.5f - 0.5f * oneLess / Smoothness).Clamp();
			return (oneLess - Smoothness + Smoothness * h) * h + 1f;
		}
	}
}