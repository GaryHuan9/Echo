using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;

namespace Echo.Core.Common.Mathematics.Primitives;

/// <summary>
/// An <see cref="origin"/> and a <see cref="direction"/> that represents a geometric ray together.
/// The reciprocal of <see cref="direction"/> is precalculated during <see cref="Ray"/> construction.
/// </summary>
public readonly struct Ray
{
	/// <summary>
	/// Constructs a <see cref="Ray"/>.
	/// </summary>
	/// <param name="origin">The origin of the ray.</param>
	/// <param name="direction">The unit direction of the ray.</param>
	public Ray(Float3 origin, Float3 direction)
	{
		Assert.AreEqual(direction.SquaredMagnitude, 1f);

		this.origin = origin;
		this.direction = direction;
		directionR = (Float3)(1f / (Float4)direction);
	}

	public readonly Float3 origin;
	public readonly Float3 direction;
	public readonly Float3 directionR;

	/// <summary>
	/// Returns the point this <see cref="Ray"/> points at <paramref name="distance"/>.
	/// </summary>
	public Float3 GetPoint(float distance) => direction * distance + origin;

	public override string ToString() => $"{nameof(origin)}: {origin}, {nameof(direction)}: {direction}";
}