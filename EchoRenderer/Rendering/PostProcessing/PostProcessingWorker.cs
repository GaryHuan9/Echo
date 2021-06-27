using System.Threading.Tasks;
using CodeHelpers.Mathematics;
using CodeHelpers.ObjectPooling;
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
		protected readonly RenderBuffer renderBuffer;

		public bool Aborted => engine.Aborted;

		/// <summary>
		/// Executes this worker's passes on <see cref="renderBuffer"/>. The worker should not assume the alpha
		/// channel on <see cref="renderBuffer"/> is 1f and also do not have to assign 1f to the final buffer.
		/// </summary>
		public abstract void Dispatch();

		public void RunPass(PassAction passAction, Texture2D buffer = null)
		{
			if (Aborted) return;

			buffer ??= renderBuffer;
			Parallel.ForEach(buffer.size.Loop(), WorkPixel);

			void WorkPixel(Int2 position, ParallelLoopState state)
			{
				if (Aborted) state.Stop();
				else passAction(position);
			}
		}

		public void RunCopyPass(Texture from, Texture2D to) => to.CopyFrom(from);

		public void RunPassHorizontal(PassActionHorizontal passAction, Texture2D buffer = null)
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

		public void RunPassVertical(PassActionVertical passAction, Texture2D buffer = null)
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

		/// <summary>
		/// Gets the handle to a temporary <see cref="Array2D"/> with the same size as <see cref="renderBuffer"/>.
		/// This method does not guarantee the initial content of the allocated buffer! It might not be empty.
		/// NOTE: Remember to use the using statement to release the texture when you are done with it!
		/// </summary>
		public ReleaseHandle<Array2D> FetchTemporaryBuffer(out Array2D buffer) => engine.texturePooler.Fetch(out buffer);

		/// <summary>
		/// Functions similarly to <see cref="FetchTemporaryBuffer(out Array2D)"/>, except that you can indicate a <paramref name="size"/>
		/// which must be smaller than or equals to the size of <see cref="renderBuffer"/>. NOTE: Remember to dispose/release the handle.
		/// </summary>
		public ReleaseHandle<Array2D> FetchTemporaryBuffer(out Texture2D buffer, Int2 size)
		{
			var handle = FetchTemporaryBuffer(out Array2D texture);

			if (size == texture.size) buffer = texture;
			else buffer = new Crop2D(texture, Int2.zero, size);

			return handle;
		}

		/// <summary>
		/// Gets the handle to a temporary <see cref="Array2D"/> with the same size as <see cref="renderBuffer"/>.
		/// The content of <see cref="renderBuffer"/> is copied onto the allocated <paramref name="buffer"/>.
		/// NOTE: Remember to use the using statement to release the texture when you are done with it!
		/// </summary>
		public ReleaseHandle<Array2D> CopyTemporaryBuffer(out Array2D buffer)
		{
			var handle = FetchTemporaryBuffer(out buffer);
			RunCopyPass(renderBuffer, buffer);

			return handle;
		}

		public delegate void PassAction(Int2 position);
		public delegate void PassActionHorizontal(int vertical);
		public delegate void PassActionVertical(int horizontal);
	}
}