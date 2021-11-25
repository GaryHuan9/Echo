using EchoRenderer.Mathematics;
using EchoRenderer.Rendering.Profiles;

namespace EchoRenderer.Rendering.Memory
{
	/// <summary>
	/// A region of memory that can be used to store localized temporary objects or access shared immutable objects.
	/// NOTE: This class should be unique/local to each thread and can be inherited for more options.
	/// Thus, the entirety of this class is not thread safe and relies on this fact for fast memory.
	/// </summary>
	public class Arena
	{
		/// <summary>
		/// Creates a new <see cref="Arena"/>.
		/// </summary>
		/// <param name="profile">The <see cref="RenderProfile"/> to use for this render.</param>
		/// <param name="seed">Should be fairly random number that varies based on each rendering thread.</param>
		public Arena(RenderProfile profile, int seed)
		{
			this.profile = profile;
			random = new ExtendedRandom(seed);
		}

		public readonly RenderProfile profile;
		public readonly ExtendedRandom random;

		public readonly Allocator allocator = new();
	}
}