using Echo.Core.Aggregation.Bounds;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Scenic.Geometric;

public interface IPreparedGeometry
{
	AxisAlignedBoundingBox AABB { get; }

	ConeBounds ConeBounds { get; }

	float GetPower(PreparedSwatch swatch);
}

public interface IPreparedPureGeometry : IPreparedGeometry
{
	MaterialIndex Material { get; }

	float Area { get; }

	float IPreparedGeometry.GetPower(PreparedSwatch swatch) => swatch[Material] is IEmissive emissive ? emissive.Power * Area : 0f;
}