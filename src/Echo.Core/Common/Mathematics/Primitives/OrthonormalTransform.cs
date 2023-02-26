using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;

namespace Echo.Core.Common.Mathematics.Primitives;

/// <summary>
/// A transform defined by three orthogonal unit vectors. Bidirectionally map directions from the canonical
/// vector space to the vector space defined by the three axes in this <see cref="OrthonormalTransform"/>.
/// </summary>
public readonly struct OrthonormalTransform
{
	public OrthonormalTransform(in Float3 axisZ)
	{
		Ensure.AreEqual(axisZ.SquaredMagnitude, 1f);

		this.axisZ = axisZ;

		if (!FastMath.AlmostZero(axisZ.X) || !FastMath.AlmostZero(axisZ.Y))
		{
			axisX = new Float3(axisZ.Y, -axisZ.X, 0f).Normalized; //Equivalent to Float3.Cross(axisZ, Float3.Forward)
			axisY = Float3.Cross(axisZ, axisX);
		}
		else
		{
			axisX = Float3.Right;
			axisY = new Float3(0f, axisZ.Z, -axisZ.Y).Normalized; //Equivalent to Float3.Cross(axisZ, axisX);
		}

		Ensure.AreEqual(axisX.SquaredMagnitude, 1f);
		Ensure.AreEqual(axisY.SquaredMagnitude, 1f);
	}

	public OrthonormalTransform(in Float3 axisZ, in Float3 axisX)
	{
		Ensure.AreEqual(axisZ.SquaredMagnitude, 1f);
		Ensure.AreEqual(axisX.SquaredMagnitude, 1f);
		Ensure.IsTrue(FastMath.AlmostZero(Float3.Dot(axisX, axisZ)));

		this.axisZ = axisZ;
		this.axisX = axisX;
		axisY = Float3.Cross(axisZ, axisX);
	}

	public readonly Float3 axisX;
	public readonly Float3 axisY;
	public readonly Float3 axisZ;

	/// <summary>
	/// Transforms a <see cref="Float3"/> vector along this <see cref="OrthonormalTransform"/>.
	/// </summary>
	public Float3 ApplyForward(in Float3 direction) => new
	(
		axisX.X * direction.X + axisY.X * direction.Y + axisZ.X * direction.Z,
		axisX.Y * direction.X + axisY.Y * direction.Y + axisZ.Y * direction.Z,
		axisX.Z * direction.X + axisY.Z * direction.Y + axisZ.Z * direction.Z
	);

	/// <summary>
	/// Transforms a <see cref="Float3"/> vector against this <see cref="OrthonormalTransform"/>.
	/// </summary>
	public Float3 ApplyInverse(in Float3 direction) => new
	(
		axisX.X * direction.X + axisX.Y * direction.Y + axisX.Z * direction.Z,
		axisY.X * direction.X + axisY.Y * direction.Y + axisY.Z * direction.Z,
		axisZ.X * direction.X + axisZ.Y * direction.Y + axisZ.Z * direction.Z
	);
}