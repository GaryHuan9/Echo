using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Core.PostProcess.Operators;

namespace EchoRenderer.Core.PostProcess.ToneMappers;

public abstract class ToneMapper : PostProcessingWorker
{
	protected ToneMapper(PostProcessingEngine engine) : base(engine) { }

	float luminanceForward;
	float luminanceInverse;

	//https://bruop.github.io/tonemapping/

	public override void Dispatch()
	{
		var grab = new LuminanceGrab(this, renderBuffer);

		grab.Run();

		if (grab.Luminance.AlmostEquals()) return;

		luminanceForward = 9.6f * grab.Luminance;
		luminanceInverse = 1f / luminanceForward;

		RunPass(MainPass);
	}

	protected abstract float MapLuminance(float luminance);

	void MainPass(Int2 position)
	{
		RGBA32 source = renderBuffer[position];
		float luminance = source.Luminance;

		float mapped = MapLuminance(luminance * luminanceInverse) * luminanceForward;
		float multiplier = FastMath.AlmostZero(luminance) ? mapped : mapped / luminance;
		renderBuffer[position] = source * multiplier;
	}
}