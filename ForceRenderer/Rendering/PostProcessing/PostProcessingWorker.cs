using System.Collections.Generic;
using System.Threading.Tasks;
using CodeHelpers.Mathematics;
using ForceRenderer.Textures;

namespace ForceRenderer.Rendering.PostProcessing
{
	public abstract class PostProcessingWorker
	{
		protected PostProcessingWorker(Texture renderBuffer) => this.renderBuffer = renderBuffer;

		protected readonly Texture renderBuffer;

		readonly List<PassAction> passes = new List<PassAction>();

		bool aborted;

		public void Dispatch()
		{
			Prepare();

			int length = renderBuffer.size.Product;
			foreach (PassAction passAction in passes)
			{
				Parallel.For(0, length, WorkPixel);

				void WorkPixel(int index, ParallelLoopState state)
				{
					if (aborted) state.Break();
					else passAction(renderBuffer.ToPosition(index));
				}

				if (aborted) break;
			}
		}

		public void Abort()
		{
			aborted = true;
		}

		/// <summary>
		/// This is where the worker should orderly add all of the passes.
		/// The pass that is added first is executed first.
		/// </summary>
		protected abstract void Prepare();

		protected void AddPass(PassAction passAction) => passes.Add(passAction);

		protected void AddCopyPass(Texture from, Texture to) => AddPass
		(
			position =>
			{
				ref var target = ref to.GetPixel(position);
				target = from.GetPixel(position);
			}
		);

		protected delegate void PassAction(Int2 position);
	}
}