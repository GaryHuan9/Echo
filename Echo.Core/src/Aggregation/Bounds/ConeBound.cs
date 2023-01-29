using System;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;

namespace Echo.Core.Aggregation.Bounds;

public readonly struct ConeBound : IFormattable
{
	ConeBound(in Float3 axis, float cosOffset = 1f, float cosExtend = 0f)
	{
		Ensure.AreEqual(axis.SquaredMagnitude, 1f);
		Ensure.IsTrue(cosOffset is >= -1f and <= 1f);
		Ensure.IsTrue(cosExtend is >= 0f and <= 1f);

		this.axis = axis;
		this.cosOffset = cosOffset;
		this.cosExtend = cosExtend;
	}

	public readonly Float3 axis;
	public readonly float cosOffset;
	public readonly float cosExtend;

	public float RelativeArea
	{
		get
		{
			Ensure.AreNotEqual(axis, default);

			float offset = MathF.Acos(FastMath.Clamp11(cosOffset));
			float extend = MathF.Acos(FastMath.Clamp11(cosExtend));

			float angle = FastMath.Min(offset + extend, Scalars.Pi) * 2f;
			float sinOffset = FastMath.Identity(cosOffset);

			return Scalars.Tau * (1f - cosOffset) + Scalars.Pi / 2f *
			(
				angle * sinOffset - MathF.Cos(offset - angle) -
				2f * offset * sinOffset + cosOffset
			);
		}
	}

	public ConeBound Encapsulate(in ConeBound other)
	{
		Ensure.AreNotEqual(axis, default);
		Ensure.AreNotEqual(other.axis, default);

		return other.cosOffset > cosOffset ? Union(this, other) : Union(other, this);
	}

	public override string ToString() => ToString(default);

	public string ToString(string format, IFormatProvider provider = null)
	{
		float offset = Scalars.ToDegrees(MathF.Acos(cosOffset));
		float extend = Scalars.ToDegrees(MathF.Acos(cosExtend));

		return $"{axis.ToString(format, provider)} ± {offset.ToString(format, provider)}° ± {extend.ToString(format, provider)}°";
	}

	public static ConeBound CreateFullSphere(float cosExtend = 0f /* = cos(pi/2) */) => new(Float3.Up, -1f /* = cos(pi) */, cosExtend);

	public static ConeBound CreateDirection(in Float3 direction, float cosExtend = 0f /* = cos(pi/2) */) => new(direction, 1f /* = cos(0) */, cosExtend);

	public static ConeBound operator *(in Float4x4 transform, ConeBound bound) => new
	(
		transform.MultiplyDirection(bound.axis).Normalized,
		bound.cosOffset, bound.cosExtend
	);

	static ConeBound Union(in ConeBound value0, in ConeBound value1)
	{
		float offset0 = MathF.Acos(FastMath.Clamp11(value0.cosOffset));
		float offset1 = MathF.Acos(FastMath.Clamp11(value1.cosOffset));
		float cosExtend = FastMath.Min(value0.cosExtend, value1.cosExtend);

		Ensure.IsTrue(offset0 >= offset1);

		Float3 axis = value0.axis;
		float max = value0.axis.Angle(value1.axis) + offset1;

		//Early exit if the offset of value0 is large enough already and do not need to extended
		if (FastMath.Min(max, Scalars.Pi) <= offset0) return new ConeBound(axis, value0.cosOffset, cosExtend);

		//Create new cone over the two inputs if the value0 is not large enough already
		float offset = offset0;
		offset = (offset + max) / 2f;

		//Clamp and return the maximum cone if offset is pi or larger
		if (offset >= Scalars.Pi) return CreateFullSphere(cosExtend);

		//Rotate axis to the middle of the two axes of the two inputs
		Float3 cross = axis.Cross(value1.axis);
		float rotation = offset - offset0;
		axis = new Versor(cross, rotation) * axis;

		return new ConeBound(axis, MathF.Cos(offset), cosExtend);
	}
}