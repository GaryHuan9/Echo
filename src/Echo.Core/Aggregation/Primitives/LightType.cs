﻿using Echo.Core.Scenic.Lights;

namespace Echo.Core.Aggregation.Primitives;

/// <summary>
/// The light type of an <see cref="EntityToken"/> if that <see cref="EntityToken"/> is of type <see cref="TokenType.Light"/>.
/// </summary>
/// <remarks>Because of the data size <see cref="EntityToken"/>, at a maximum there can only be 64
/// different <see cref="LightType"/>. See <see cref="EntityToken.LightIndexCount"/> for more.</remarks>
public enum LightType : uint
{
	/// <summary>
	/// Represents an <see cref="InfiniteLight"/>.
	/// </summary>
	Infinite,

	/// <summary>
	/// Identical to <see cref="Infinite"/> except the light is Dirac delta
	/// </summary>
	InfiniteDelta,

	/// <summary>
	/// Represents a <see cref="PreparedPointLight"/>.
	/// </summary>
	Point
}