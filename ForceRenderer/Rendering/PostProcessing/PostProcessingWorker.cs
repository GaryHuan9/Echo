using System.Collections.Generic;
using System.Threading.Tasks;
using CodeHelpers.Files;
using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
using ForceRenderer.Textures;

namespace ForceRenderer.Rendering.PostProcessing
{
	public abstract class PostProcessingWorker
	{
		protected PostProcessingWorker(PostProcessingEngine engine)
		{
			this.engine = engine;
			renderBuffer = engine.renderBuffer;
		}

		public readonly PostProcessingEngine engine;
		protected readonly Texture renderBuffer;

		public bool Aborted => engine.Aborted;

		public abstract void Dispatch();

		protected void RunPass(PassAction passAction)
		{
			if (Aborted) return;
			Parallel.For(0, renderBuffer.size.Product, WorkPixel);

			void WorkPixel(int index, ParallelLoopState state)
			{
				if (Aborted) state.Break();
				else passAction(renderBuffer.ToPosition(index));
			}
		}

		protected void RunCopyPass(Texture from, Texture to) => RunPass(position => to[position] = from[position]);

		protected void RunPassHorizontal(PassActionHorizontal passAction)
		{
			if (Aborted) return;

			Parallel.For(0, renderBuffer.size.y, WorkPixel);

			void WorkPixel(int vertical, ParallelLoopState state)
			{
				if (Aborted) state.Break();
				else passAction(vertical);
			}
		}

		protected void RunPassVertical(PassActionVertical passAction)
		{
			if (Aborted) return;

			Parallel.For(0, renderBuffer.size.x, WorkPixel);

			void WorkPixel(int horizontal, ParallelLoopState state)
			{
				if (Aborted) state.Break();
				else passAction(horizontal);
			}
		}

		protected delegate void PassAction(Int2 position);
		protected delegate void PassActionHorizontal(int vertical);
		protected delegate void PassActionVertical(int horizontal);
	}
}