using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;

namespace Echo.Common.Mathematics.Primitives;

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

		this.origin = (Float4)origin;
		this.direction = (Float4)direction;
		directionR = 1f / this.direction;
	}

	public readonly Float4 origin;
	public readonly Float4 direction;
	public readonly Float4 directionR;

	public Float3 Origin => (Float3)origin;
	public Float3 Direction => (Float3)direction;
	public Float3 DirectionR => (Float3)directionR;

	/// <summary>
	/// Returns the point this <see cref="Ray"/> points at <paramref name="distance"/>.
	/// </summary>
	public Float3 GetPoint(float distance) => (Float3)(direction * distance + origin);

	public override string ToString() => $"{nameof(origin)}: {(Float3)origin}, {nameof(direction)}: {(Float3)direction}";
}