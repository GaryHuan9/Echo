using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.PostProcessing.ToneMappers
{
	public class BasicShoulder : ToneMapper
	{
		public BasicShoulder(PostProcessingEngine engine, float smoothness) : base(engine) => this.smoothness = smoothness;

		readonly float smoothness;

		//https://www.desmos.com/calculator/nngw01x7om

		protected override float MapLuminance(float luminance)
		{
			float oneLess = luminance - 1f;

			float h = (0.5f - 0.5f * oneLess / smoothness).Clamp();
			return (oneLess - smoothness + smoothness * h) * h + 1f;
		}
	}
}