using EchoRenderer.Mathematics;

namespace EchoRenderer.Rendering
{
	/// <summary>
	/// A class that handles memory allocation and deallocation during the rendering of a sample.
	/// NOTE: This class should be unique to each thread and can be inherited for more options.
	/// </summary>
	public class MemoryArena
	{
		/// <summary>
		/// Creates a new <see cref="MemoryArena"/>.
		/// </summary>
		/// <param name="hash">Should be fairly random number that varies based on each rendering thread.</param>
		public MemoryArena(int hash) => random = new ExtendedRandom(hash);

		public readonly ExtendedRandom random;
	}
}