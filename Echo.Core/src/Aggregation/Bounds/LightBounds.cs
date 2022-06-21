using CodeHelpers.Packed;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;

namespace Echo.Core.Aggregation.Bounds;

public readonly struct LightBounds
{
	public LightBounds(in AxisAlignedBoundingBox aabb, in ConeBounds cone, float energy)
	{
		this.aabb = aabb;
		this.cone = cone;
		this.energy = energy;
	}

	public readonly AxisAlignedBoundingBox aabb;
	public readonly ConeBounds cone;
	public readonly float energy;

	public float Area => aabb.HalfArea * cone.Area * energy; //relative

	public LightBounds Encapsulate(in LightBounds other) => new
	(
		aabb.Encapsulate(other.aabb),
		cone.Encapsulate(other.cone),
		energy + other.energy
	);

	public float Importance(in GeometryPoint point)
	{
		Float3 center = aabb.Center;
		Float3 incident = center - point;

		float length2 = incident.SquaredMagnitude;

		if (FastMath.AlmostZero(length2)) incident = Float3.Zero;
		else incident *= FastMath.SqrtR0(length2); //Normalized

		//TODO: clamp length2
		float length2R = 1f / length2;

		float cosAxis = -cone.axis.Dot(incident);
		float sinAxis = FastMath.Identity(cosAxis);

		float radius2 = aabb.max.SquaredDistance(center);
		float sinRadius2 = radius2 * length2R;
		float sinRadius = FastMath.Sqrt0(sinRadius2);
		float cosRadius = FastMath.Sqrt0(1f - sinRadius2);

		float cosOffset = cone.cosOffset;
		float sinOffset = FastMath.Identity(cosOffset);

		float cosRemain = ClampSubtractCos(sinAxis, cosAxis, sinOffset, cosOffset);
		float sinRemain = ClampSubtractSin(sinAxis, cosAxis, sinOffset, cosOffset);
		float cosFinal = ClampSubtractCos(sinRemain, cosRemain, sinRadius, cosRadius);
		if (cosFinal <= cone.cosExtend) return 0f;

		float cosIncident = point.normal.Dot(incident);
		float sinIncident = FastMath.Identity(cosIncident);
		float cosReflect = ClampSubtractCos(sinIncident, cosIncident, sinRadius, cosRadius);

		return FastMath.Max0(energy * cosFinal * length2R * cosReflect);
	}

	static float ClampSubtractCos(float sin0, float cos0, float sin1, float cos1) => cos0 > cos1 ? 1f : cos0 * cos1 + sin0 * sin1;
	static float ClampSubtractSin(float sin0, float cos0, float sin1, float cos1) => cos0 > cos1 ? 0f : sin0 * cos1 - cos0 * sin1;
}