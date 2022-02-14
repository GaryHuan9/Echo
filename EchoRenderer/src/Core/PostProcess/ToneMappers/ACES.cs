using CodeHelpers.Mathematics;

namespace EchoRenderer.Core.PostProcess.ToneMappers;

public class ACES : ToneMapper
{
	public ACES(PostProcessingEngine engine) : base(engine) { }

	//https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/

	protected override float MapLuminance(float luminance)
	{
		float mad0 = 2.51f * luminance + 0.03f;
		float mad1 = 2.43f * luminance + 0.59f;

		return (luminance * mad0 / (luminance * mad1 + 0.14f)).Clamp();
	}
}