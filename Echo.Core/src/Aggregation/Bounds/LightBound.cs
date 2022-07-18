using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;

namespace Echo.Core.Aggregation.Bounds;

public readonly struct LightBound
{
	public LightBound(in BoxBound box, in ConeBound cone, float power)
	{
		this.box = box;
		this.cone = cone;
		this.power = power;
	}

	public readonly BoxBound box;
	public readonly ConeBound cone;
	public readonly float power;

	public float Area => box.HalfArea * cone.Area * power; //relative

	public LightBound Encapsulate(in LightBound other) => new
	(
		box.Encapsulate(other.box),
		cone.Encapsulate(other.cone),
		power + other.power
	);

	public float Importance(in GeometryPoint origin)
	{
		Float3 center = box.Center;
		Float3 incident = origin - center;

		float length2 = incident.SquaredMagnitude;

		if (FastMath.AlmostZero(length2)) incident = Float3.Zero;
		else incident *= FastMath.SqrtR0(length2); //Normalized

		float cosAxis = cone.axis.Dot(incident);
		float sinAxis = FastMath.Identity(cosAxis);

		float cosOffset = cone.cosOffset;
		float sinOffset = FastMath.Identity(cosOffset);

		FindSubtendedAngles(box, length2, out float sinRadius, out float cosRadius);

		float cosRemain = ClampSubtractCos(sinAxis, cosAxis, sinOffset, cosOffset);
		float sinRemain = ClampSubtractSin(sinAxis, cosAxis, sinOffset, cosOffset);
		float cosFinal = ClampSubtractCos(sinRemain, cosRemain, sinRadius, cosRadius);
		if (cosFinal <= cone.cosExtend) return 0f;

		float cosIncident = FastMath.Abs(origin.normal.Dot(incident));
		float sinIncident = FastMath.Identity(cosIncident);
		float cosReflect = ClampSubtractCos(sinIncident, cosIncident, sinRadius, cosRadius);

		//TODO: clamp length2
		return FastMath.Max0(power / length2 * cosFinal * cosReflect);
	}

	static void FindSubtendedAngles(in BoxBound boxBound, float length2, out float sin, out float cos)
	{
		float radius2 = (boxBound.max - boxBound.min).SquaredMagnitude / 4f;

		if (length2 < radius2)
		{
			sin = 0f;
			cos = -1f;
		}
		else
		{
			float sinRadius2 = radius2 / length2;
			sin = FastMath.Sqrt0(sinRadius2);
			cos = FastMath.Sqrt0(1f - sinRadius2);
		}
	}

	static float ClampSubtractCos(float sin0, float cos0, float sin1, float cos1) => cos0 > cos1 ? 1f : cos0 * cos1 + sin0 * sin1;
	static float ClampSubtractSin(float sin0, float cos0, float sin1, float cos1) => cos0 > cos1 ? 0f : sin0 * cos1 - cos0 * sin1;
}