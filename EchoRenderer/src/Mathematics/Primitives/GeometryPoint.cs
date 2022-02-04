using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Mathematics.Primitives;

/// <summary>
/// Contains geometric information about a point on a geometry.
/// </summary>
public readonly struct GeometryPoint
{
	public GeometryPoint(in Float3 position, in Float3 normal)
	{
		Assert.AreEqual(normal.SquaredMagnitude, 1f);

		this.position = position;
		this.normal = normal;
	}

	/// <summary>
	/// The world space position of this point.
	/// </summary>
	public readonly Float3 position;

	/// <summary>
	/// The world space surface normal of the geometry at this point.
	/// </summary>
	public readonly Float3 normal;

	/// <summary>
	/// Returns the uniform probability density function (pdf) of this <see cref="GeometryPoint"/>, which
	/// is from a geometry with <paramref name="area"/>, over solid angle at <paramref name="origin"/>.
	/// </summary>
	public float ProbabilityDensity(in Float3 origin, float area)
	{
		Float3 offset = position - origin;
		return offset.SquaredMagnitude / FastMath.Abs(normal.Dot(offset.Normalized) * area);
	}

	/// <summary>
	/// Transforms this <see cref="GeometryPoint"/> by <paramref name="transform"/>.
	/// </summary>
	public static GeometryPoint operator *(in Float4x4 transform, in GeometryPoint point) => new
	(
		transform.MultiplyPoint(point.position),
		transform.MultiplyDirection(point.normal).Normalized
	);
}