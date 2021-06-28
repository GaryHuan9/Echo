using EchoRenderer.Rendering.PostProcessing.Operators;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.PostProcessing
{
	public class FieldOfView : PostProcessingWorker
	{
		public FieldOfView(PostProcessingEngine engine) : base(engine) { }

		float deviation;

		public override void Dispatch()
		{
			using var handle = CopyTemporaryBuffer(out Array2D workerBuffer);
			using var blur = new GaussianBlur(this, workerBuffer, deviation);
		}
	}
}