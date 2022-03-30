using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;

namespace EchoRenderer.Common.Mathematics.Primitives;

/// <summary>
/// A transform constructed from a surface normal. Can be used to transform a direction between local and world space.
/// </summary>
public readonly struct NormalTransform
{
	public NormalTransform(in Float3 normal)
	{
		this.normal = normal;

		Float3 helper = FastMath.Abs(normal.X) >= 0.9f ? Float3.Forward : Float3.Right;

		tangent = Float3.Cross(normal, helper).Normalized;
		binormal = Float3.Cross(normal, tangent);

		Assert.AreEqual(normal.SquaredMagnitude, 1f);
		Assert.AreEqual(binormal.SquaredMagnitude, 1f);
	}

	public readonly Float3 normal;

	readonly Float3 tangent;
	readonly Float3 binormal;

	/// <summary>
	/// Transforms a <paramref name="direction"/> from world space to local space using this <see cref="NormalTransform"/>.
	/// </summary>
	public Float3 WorldToLocal(in Float3 direction) => new(direction.Dot(tangent), direction.Dot(binormal), direction.Dot(normal));

	/// <summary>
	/// Transforms a <paramref name="direction"/> from local space to world space using this <see cref="NormalTransform"/>.
	/// </summary>
	public Float3 LocalToWorld(in Float3 direction) => new
	(
		tangent.X * direction.X + binormal.X * direction.Y + normal.X * direction.Z,
		tangent.Y * direction.X + binormal.Y * direction.Y + normal.Y * direction.Z,
		tangent.Z * direction.X + binormal.Z * direction.Y + normal.Z * direction.Z
	);
}