using System;
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
		public Arena(RenderProfile profile) => this.profile = profile;

		public readonly RenderProfile profile;
		public readonly Allocator allocator = new();

		Distribution distribution;
		IRandom _random;

		public Distribution Distribution
		{
			get => distribution;
			set
			{
				Assert.IsNotNull(distribution);
				Assert.IsNotNull(value);

				distribution = value.Replicate();
				distribution.PRNG = _random;
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
				if (distribution != null) distribution.PRNG = value;
			}
		}
	}
}