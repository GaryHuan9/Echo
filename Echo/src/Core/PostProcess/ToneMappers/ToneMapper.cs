using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Common.Mathematics;
using Echo.Core.PostProcess.Operators;
using Echo.Core.Textures.Colors;

namespace Echo.Core.PostProcess.ToneMappers;

public abstract class ToneMapper : PostProcessingWorker
{
	protected ToneMapper(PostProcessingEngine engine) : base(engine) { }

	float luminanceForward;
	float luminanceInverse;

	//https://bruop.github.io/tonemapping/

	public override void Dispatch()
	{
		using var grab = new LuminanceGrab(this, renderBuffer);

		grab.Run();

		if (grab.Luminance.AlmostEquals()) return;

		luminanceForward = 9.6f * grab.Luminance;
		luminanceInverse = 1f / luminanceForward;

		RunPass(MainPass);
	}

	protected abstract float MapLuminance(float luminance);

	void MainPass(Int2 position)
	{
		RGB128 source = renderBuffer[position];
		float luminance = source.Luminance;

		float mapped = MapLuminance(luminance * luminanceInverse) * luminanceForward;
		float multiplier = FastMath.AlmostZero(luminance) ? mapped : mapped / luminance;
		renderBuffer[position] = source * multiplier;
	}
}