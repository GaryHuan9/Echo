using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Mathematics;
using EchoRenderer.PostProcess.Operators;

namespace EchoRenderer.PostProcess.ToneMappers;

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
		Vector128<float> source = renderBuffer[position];
		float luminance = PackedMath.GetLuminance(source);

		float mapped = MapLuminance(luminance * luminanceInverse) * luminanceForward;
		float multiplier = FastMath.AlmostZero(luminance) ? mapped : mapped / luminance;
		renderBuffer[position] = Sse.Multiply(source, Vector128.Create(multiplier));
	}
}