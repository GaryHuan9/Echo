using CodeHelpers.Diagnostics;
using EchoRenderer.Common.Mathematics.Randomization;
using EchoRenderer.Core.Rendering.Distributions;

namespace EchoRenderer.Common.Memory;

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
	public Arena(ContinuousDistribution distribution)
	{
		Assert.IsNotNull(distribution);
		this.distribution = distribution;
	}

	public readonly Allocator allocator = new();
	public readonly ContinuousDistribution distribution;

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