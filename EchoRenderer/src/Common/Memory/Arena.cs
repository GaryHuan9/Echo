using CodeHelpers;
using EchoRenderer.Core.Rendering.Distributions.Continuous;

namespace EchoRenderer.Common.Memory;

/// <summary>
/// A region of memory that can be used to store localized temporary objects or access shared immutable objects.
/// NOTE: This class should be unique/local to each thread and can be inherited for more options.
/// Thus, the entirety of this class is not thread safe and relies on this fact for fast memory.
/// </summary>
public class Arena
{
	/// <summary>
	/// An <see cref="Allocator"/> associated with this <see cref="Arena"/>.
	/// </summary>
	public readonly Allocator allocator = new();

	NotNull<ContinuousDistribution> _distribution;

	/// <summary>
	/// A <see cref="ContinuousDistribution"/> associated with this <see cref="Arena"/>.
	/// </summary>
	public ContinuousDistribution Distribution
	{
		get => _distribution;
		set => _distribution = value;
	}
}