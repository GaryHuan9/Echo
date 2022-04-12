using System.Threading.Tasks;
using CodeHelpers.Packed;
using CodeHelpers.Pooling;
using EchoRenderer.Common.Coloring;
using EchoRenderer.Core.Texturing;
using EchoRenderer.Core.Texturing.Grid;

namespace EchoRenderer.Core.PostProcess;

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

	/// <summary>
	/// Run <paramref name="passAction"/> through every integer position on <paramref name="buffer"/>.
	/// NOTE: If <paramref name="buffer"/> is null, <see cref="renderBuffer"/> will be used instead.
	/// </summary>
	public void RunPass(PassAction passAction, TextureGrid buffer = null)
	{
		if (Aborted) return;
		buffer ??= renderBuffer;

		Parallel.ForEach(buffer.size.Loop(), (position, state) =>
		{
			if (Aborted) state.Break();
			else passAction(position);
		});
	}

	/// <summary>
	/// Run a pass along every horizontal position on <paramref name="buffer"/>.
	/// </summary>
	public void RunPassHorizontal(PassActionHorizontal passAction, TextureGrid buffer = null)
	{
		if (Aborted) return;
		buffer ??= renderBuffer;

		Parallel.For(0, buffer.size.X, (horizontal, state) =>
		{
			if (Aborted) state.Break();
			else passAction(horizontal);
		});
	}

	/// <summary>
	/// Run a pass along every vertical position on <paramref name="buffer"/>.
	/// </summary>
	public void RunPassVertical(PassActionVertical passAction, TextureGrid buffer = null)
	{
		if (Aborted) return;
		buffer ??= renderBuffer;

		Parallel.For(0, buffer.size.Y, (vertical, state) =>
		{
			if (Aborted) state.Break();
			else passAction(vertical);
		});
	}

	/// <summary>
	/// Gets the handle to a temporary <see cref="ArrayGrid"/> with the same size as <see cref="renderBuffer"/>.
	/// NOTE: This method does not make any guarantee to the initial content of the output buffer!
	/// NOTE: Remember to use the using statement to release the texture when you are done with it!
	/// </summary>
	public ReleaseHandle<ArrayGrid> FetchTemporaryBuffer(out ArrayGrid buffer) => engine.texturePooler.Fetch(out buffer);

	/// <summary>
	/// Functions similarly to <see cref="FetchTemporaryBuffer(out ArrayGrid{RGB128})"/>, except that you can indicate a <paramref name="size"/>
	/// which must be smaller than or equals to the size of <see cref="renderBuffer"/>. NOTE: Remember to dispose/release the handle.
	/// </summary>
	public ReleaseHandle<ArrayGrid> FetchTemporaryBuffer(out TextureGrid buffer, Int2 size)
	{
		var handle = FetchTemporaryBuffer(out ArrayGrid texture);

		if (size == texture.size) buffer = texture;
		else buffer = new CropGrid(texture, Int2.Zero, size);

		return handle;
	}

	/// <summary>
	/// Gets the handle to a temporary <see cref="ArrayGrid"/> with the same size as <see cref="renderBuffer"/>.
	/// The content of <see cref="renderBuffer"/> is copied onto the allocated <paramref name="buffer"/>.
	/// NOTE: Remember to use the using statement to release the texture when you are done with it!
	/// </summary>
	public ReleaseHandle<ArrayGrid> CopyTemporaryBuffer(out ArrayGrid buffer)
	{
		var handle = FetchTemporaryBuffer(out buffer);
		buffer.CopyFrom(renderBuffer);

		return handle;
	}

	public delegate void PassAction(Int2 position);
	public delegate void PassActionHorizontal(int horizontal);
	public delegate void PassActionVertical(int vertical);
}