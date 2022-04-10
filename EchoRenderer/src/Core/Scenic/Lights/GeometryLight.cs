using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Coloring;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Distributions;
using EchoRenderer.Core.Rendering.Materials;
using EchoRenderer.Core.Scenic.Preparation;

namespace EchoRenderer.Core.Scenic.Lights;

/// <summary>
/// An <see cref="IAreaLight"/> that can contain an emissive geometry. Note that this light cannot be contained in a specific scene,
/// since it is dynamically created when we sample different <see cref="ILight"/> during rendering. This <see cref="GeometryLight"/>
/// thus allows us to sample emissive geometries through the same <see cref="ILight"/> interface.
/// </summary>
public class GeometryLight : IAreaLight
{
	public void Reset(PreparedScene newScene, in GeometryToken newToken, IEmissive newEmissive)
	{
		scene = newScene;
		token = newToken;
		Emissive = newEmissive;

		Assert.IsTrue(FastMath.Positive(Emissive.Power));
	}

	PreparedScene scene;
	GeometryToken token;

	/// <summary>
	/// The current <see cref="IEmissive"/> material that is assigned to this <see cref="GeometryLight"/>.
	/// </summary>
	public IEmissive Emissive { get; private set; }

	/// <summary>
	/// Accesses the <see cref="GeometryToken"/> that this <see cref="GeometryLight"/> currently represents.
	/// </summary>
	public ref readonly GeometryToken Token
	{
		get
		{
			Assert.IsNotNull(scene);
			return ref token;
		}
	}

	/// <inheritdoc/>
	public Probable<RGB128> Sample(in GeometryPoint point, Sample2D sample, out Float3 incident, out float travel)
	{
		Probable<GeometryPoint> sampled = scene.Sample(token, point, sample);

		Float3 delta = sampled.content.position - point;
		float travel2 = delta.SquaredMagnitude;

		if (!FastMath.Positive(travel2))
		{
			incident = Float3.Zero;
			travel = 0f;
			return Probable<RGB128>.Zero;
		}

		travel = FastMath.Sqrt0(travel2);
		incident = delta * (1f / travel);

		return (Emissive.Emit(sampled, -incident), 1f);
	}

	/// <inheritdoc/>
	public float ProbabilityDensity(in GeometryPoint point, in Float3 incident) => scene.ProbabilityDensity(token, point, incident);
}