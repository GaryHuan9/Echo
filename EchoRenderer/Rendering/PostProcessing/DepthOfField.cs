using EchoRenderer.Rendering.PostProcessing.Operators;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.PostProcessing
{
	public class DepthOfField : PostProcessingWorker
	{
		public DepthOfField(PostProcessingEngine engine) : base(engine) { }

		float deviation;

		public override void Dispatch()
		{
			using var handle = CopyTemporaryBuffer(out Array2D workerBuffer);
			using var blur = new GaussianBlur(this, workerBuffer, deviation);
		}
	}
}