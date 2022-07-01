using Echo.Core.Scenic.Lighting;

namespace Echo.Core.Aggregation.Primitives;

public enum LightType : byte
{
	/// <summary>
	/// Represents an <see cref="InfiniteLight"/>.
	/// </summary>
	Infinite,

	/// <summary>
	/// Represents a <see cref="PreparedPointLight"/>.
	/// </summary>
	Point
}