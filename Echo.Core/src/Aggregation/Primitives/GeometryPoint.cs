using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Common.Mathematics;

namespace Echo.Core.Aggregation.Primitives;

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
	/// The world-space position of this point.
	/// </summary>
	public readonly Float3 position;

	/// <summary>
	/// The world-space surface normal of the geometry at this point.
	/// </summary>
	public readonly Float3 normal;

	/// <summary>
	/// Returns the uniform probability density function (pdf) of sampling this <see cref="GeometryPoint"/>,
	/// which is from a geometry with <paramref name="area"/>, over solid angles at <paramref name="origin"/>.
	/// </summary>
	public float ProbabilityDensity(in Float3 origin, float area)
	{
		Assert.IsTrue(FastMath.Positive(area));

		Float3 delta = position - origin;
		float length2 = delta.SquaredMagnitude;
		float length = FastMath.Sqrt0(length2);

		float dot = FastMath.Abs(normal.Dot(delta));
		if (!FastMath.Positive(dot)) return 0f;
		return length2 * length / (dot * area);
	}

	/// <summary>
	/// Transforms this <see cref="GeometryPoint"/> by <paramref name="transform"/>.
	/// </summary>
	public static GeometryPoint operator *(in Float4x4 transform, in GeometryPoint point) => new
	(
		transform.MultiplyPoint(point.position),
		transform.MultiplyDirection(point.normal).Normalized
	);

	/// <summary>
	/// Implicitly converts to <paramref name="point"/> to <see cref="GeometryPoint.position"/>.
	/// </summary>
	public static implicit operator Float3(in GeometryPoint point) => point.position;
}