using System.Threading.Tasks;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.PostProcessing
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

		public void RunPass(PassAction passAction, Texture buffer = null)
		{
			if (Aborted) return;

			buffer ??= renderBuffer;
			Parallel.For(0, buffer.size.Product, WorkPixel);

			void WorkPixel(int index, ParallelLoopState state)
			{
				if (Aborted) state.Stop();
				else passAction(buffer.ToPosition(index));
			}
		}

		public void RunCopyPass(Texture from, Texture to)
		{
			Assert.AreEqual(from.size, to.size);

			RunPass
			(
				position =>
				{
					ref var target = ref to.GetPixel(position);
					target = from.GetPixel(position);
				}
			);
		}

		public void RunPassHorizontal(PassActionHorizontal passAction, Texture buffer = null)
		{
			if (Aborted) return;

			buffer ??= renderBuffer;
			Parallel.For(0, buffer.size.y, WorkPixel);

			void WorkPixel(int vertical, ParallelLoopState state)
			{
				if (Aborted) state.Stop();
				else passAction(vertical);
			}
		}

		public void RunPassVertical(PassActionVertical passAction, Texture buffer = null)
		{
			if (Aborted) return;

			buffer ??= renderBuffer;
			Parallel.For(0, buffer.size.x, WorkPixel);

			void WorkPixel(int horizontal, ParallelLoopState state)
			{
				if (Aborted) state.Stop();
				else passAction(horizontal);
			}
		}

		public delegate void PassAction(Int2 position);
		public delegate void PassActionHorizontal(int vertical);
		public delegate void PassActionVertical(int horizontal);
	}
}