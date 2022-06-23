namespace Echo.Core.Aggregation.Preparation;

public class LightCollection
{
	public LightCollection(GeometryCollection geometries) => this.geometries = geometries;

	readonly GeometryCollection geometries;
}