using Echo.Core.Aggregation.Acceleration;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Scenic.Geometric;

namespace Echo.Core.Aggregation.Primitives;

/// <summary>
/// The type of an <see cref="EntityToken"/>.
/// </summary>
/// <remarks>Because of the data size <see cref="EntityToken"/>, at a maximum there can only be 16
/// different <see cref="TokenType"/>. See <see cref="EntityToken.IndexCount"/> for more.</remarks>
public enum TokenType : uint
{
	/// <summary>
	/// Represents a node inside an <see cref="Accelerator"/>.
	/// </summary>
	Node,

	/// <summary>
	/// Represents a <see cref="PreparedTriangle"/>.
	/// </summary>
	Triangle,

	/// <summary>
	/// Represents a <see cref="PreparedSphere"/>.
	/// </summary>
	Sphere,

	/// <summary>
	/// Represents a <see cref="PreparedInstance"/>.
	/// </summary>
	Instance,

	/// <summary>
	/// Represents any kind of a non-infinite light, excluding emissive geometries.
	/// </summary>
	Light
}