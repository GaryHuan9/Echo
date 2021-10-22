using EchoRenderer.Mathematics;

namespace EchoRenderer.Rendering.Memory
{
	/// <summary>
	/// A class that handles memory allocation and deallocation during the rendering of a sample.
	/// NOTE: This class should be unique to each thread and can be inherited for more options.
	/// Thus, the entirety of this class is not thread safe and relies on this fact for fast memory.
	/// </summary>
	public class Arena
	{
		/// <summary>
		/// Creates a new <see cref="Arena"/>.
		/// </summary>
		/// <param name="hash">Should be fairly random number that varies based on each rendering thread.</param>
		public Arena(int hash) => random = new ExtendedRandom(hash);

		public readonly ExtendedRandom random;
	}
}