using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;

namespace Echo.Core.Common.Mathematics.Primitives;

/// <summary>
/// A transform defined by three unit vectors that are orthogonal to each other.
/// Can be used to transform a direction between local and world-space.
/// </summary>
public readonly struct OrthonormalTransform
{
	public OrthonormalTransform(in Float3 normal)
	{
		Ensure.AreEqual(normal.SquaredMagnitude, 1f);

		this.normal = normal;

		tangent = FastMath.Abs(normal.X) > 0.9f ?
			new Float3(normal.Y, -normal.X, 0f) :
			new Float3(0f, normal.Z, -normal.Y);

		tangent = tangent.Normalized;
		binormal = Float3.Cross(normal, tangent);

		Ensure.AreEqual(tangent.SquaredMagnitude, 1f);
		Ensure.AreEqual(binormal.SquaredMagnitude, 1f);
	}

	public readonly Float3 normal;

	readonly Float3 tangent;
	readonly Float3 binormal;

	/// <summary>
	/// Transforms a <paramref name="direction"/> from world-space to local-space using this <see cref="OrthonormalTransform"/>.
	/// </summary>
	public Float3 WorldToLocal(in Float3 direction) => new(direction.Dot(tangent), direction.Dot(binormal), direction.Dot(normal));

	/// <summary>
	/// Transforms a <paramref name="direction"/> from local-space to world-space using this <see cref="OrthonormalTransform"/>.
	/// </summary>
	public Float3 LocalToWorld(in Float3 direction) => new
	(
		tangent.X * direction.X + binormal.X * direction.Y + normal.X * direction.Z,
		tangent.Y * direction.X + binormal.Y * direction.Y + normal.Y * direction.Z,
		tangent.Z * direction.X + binormal.Z * direction.Y + normal.Z * direction.Z
	);
}