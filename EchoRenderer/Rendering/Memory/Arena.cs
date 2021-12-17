using CodeHelpers.Diagnostics;
using EchoRenderer.Mathematics.Randomization;
using EchoRenderer.Rendering.Profiles;
using EchoRenderer.Rendering.Distributions;

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
		public Arena(RenderProfile profile, Distribution distribution)
		{
			Assert.IsNotNull(profile);
			Assert.IsNotNull(distribution);

			this.profile = profile;
			this.distribution = distribution;
		}

		public readonly Allocator allocator = new();
		public readonly RenderProfile profile;
		public readonly Distribution distribution;
		public PressedScene Scene => profile.Scene;

		IRandom _random;

		public IRandom Random
		{
			get => _random;
			set
			{
				Assert.IsNull(_random);
				Assert.IsNotNull(value);

				_random = value;
				distribution.Random = value;
			}
		}
	}
}