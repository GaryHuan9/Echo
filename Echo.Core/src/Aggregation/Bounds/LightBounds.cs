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
}