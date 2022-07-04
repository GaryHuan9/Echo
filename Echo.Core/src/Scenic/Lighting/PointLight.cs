using System.Runtime.CompilerServices;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Lighting;

public class PointLight : LightEntity, ILightSource<PreparedPointLight>
{
	public PreparedPointLight Extract() => new(Intensity, ContainedPosition);
}

public readonly struct PreparedPointLight : IPreparedLight
{
	public PreparedPointLight(in RGB128 intensity, in Float3 position)
	{
		this.intensity = intensity;
		this.position = position;
		energy = 4f * Scalars.Pi * intensity.Luminance;
	}

	readonly RGB128 intensity;
	readonly Float3 position;
	readonly float energy;

	public AxisAlignedBoundingBox BoxBounds => new(position, position);

	public ConeBounds ConeBounds => ConeBounds.CreateFullSphere();

	public LightBounds LightBounds => new(BoxBounds, ConeBounds, energy);

	/// <inheritdoc/>
	[SkipLocalsInit]
	public Probable<RGB128> Sample(in GeometryPoint origin, Sample2D sample, out Float3 incident, out float travel)
	{
		Float3 offset = position - origin;
		float travel2 = offset.SquaredMagnitude;

		if (!FastMath.Positive(travel2))
		{
			Unsafe.SkipInit(out incident);
			Unsafe.SkipInit(out travel);
			return Probable<RGB128>.Impossible;
		}

		travel = FastMath.Sqrt0(travel2);

		float travelR = 1f / travel;
		incident = offset * travelR;

		return new Probable<RGB128>(intensity * travelR * travelR, 1f);
	}
}