using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Common.Mathematics;

namespace Echo.Core.Aggregation.Bounds;

public readonly struct OrientationBounds
{
	public OrientationBounds(in Float3 axis, float extend, float extra)
	{
		Assert.AreEqual(axis.SquaredMagnitude, 1f);
		Assert.IsTrue(extend >= 0f);
		Assert.IsTrue(extra >= 0f);
		Assert.IsTrue(extend <= Scalars.Pi);
		Assert.IsTrue(extra <= Scalars.Pi / 2f);

		this.axis = axis;
		this.extend = extend;
		this.extra = extra;
	}

	public readonly Float3 axis;
	public readonly float extend;
	public readonly float extra;

	public OrientationBounds Encapsulate(in OrientationBounds other) => other.extend > extend ? Union(other, this) : Union(this, other);

	static OrientationBounds Union(in OrientationBounds value0, in OrientationBounds value1)
	{
		Assert.IsTrue(value0.extend >= value1.extend);

		Float3 axis = value0.axis;
		float extend = value0.extend;
		float extra = FastMath.Max(value0.extra, value1.extra);
		float max = value0.axis.Angle(value1.axis) + value1.extend;

		if (FastMath.Min(max, Scalars.Pi) > extend)
		{
			//Create new cone over the two inputs if the value0 is not large enough already
			extend = (extend + max) / 2f;

			if (extend < Scalars.Pi)
			{
				//Rotate axis to the middle of the two axes of the two inputs
				Float3 cross = axis.Cross(value1.axis);
				float rotation = extend - value0.extend;
				axis = new Versor(cross, rotation) * axis;
			}
			else extend = Scalars.Pi;
		}

		return new OrientationBounds(axis, extend, extra);
	}
}