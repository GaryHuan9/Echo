using System;
using CodeHelpers.Diagnostics;
using EchoRenderer.Mathematics.Randomization;
using EchoRenderer.Rendering.Profiles;
using EchoRenderer.Rendering.Sampling;

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
		public Arena(RenderProfile profile) => this.profile = profile;

		public readonly RenderProfile profile;
		public readonly Allocator allocator = new();

		Sampler _sampler;
		IRandom _random;

		public Sampler Sampler
		{
			get => _sampler;
			set
			{
				Assert.IsNotNull(_sampler);
				Assert.IsNotNull(value);

				_sampler = value.Replicate();
				_sampler.PRNG = _random;
			}
		}

		public IRandom Random
		{
			get => _random;
			set
			{
				Assert.IsNotNull(_random);
				Assert.IsNotNull(value);

				_random = value;
				if (_sampler != null) _sampler.PRNG = value;
			}
		}
	}
}