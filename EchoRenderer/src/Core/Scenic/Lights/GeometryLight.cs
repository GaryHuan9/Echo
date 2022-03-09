using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics;
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
	public void Reset(PreparedScene newScene, in GeometryToken newToken, in Material newMaterial)
	{
		Assert.IsTrue(newMaterial.IsEmissive);

		scene = newScene;
		token = newToken;
		emission = newMaterial.Emission;
	}

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

	PreparedScene scene;
	GeometryToken token;
	Float3 emission;

	/// <summary>
	/// Evaluates this <see cref="GeometryLight"/> at the intersected <paramref name="point"/> from incoming <paramref name="outgoing"/> direction.
	/// </summary>
	public Float3 Evaluate(in GeometryPoint point, in Float3 outgoing) => Evaluate(point, outgoing, emission);

	/// <inheritdoc/>
	public Float3 Sample(in GeometryPoint point, Sample2D sample, out Float3 incident, out float pdf, out float travel)
	{
		GeometryPoint sampled = scene.Sample(token, point, sample, out pdf);

		Float3 delta = sampled.position - point;
		float travel2 = delta.SquaredMagnitude;

		if (!FastMath.Positive(travel2))
		{
			incident = default;
			pdf = travel = default;
			return Float3.zero;
		}

		travel = FastMath.Sqrt0(travel2);
		incident = delta * (1f / travel);
		return Evaluate(sampled, incident);
	}

	/// <inheritdoc/>
	public float ProbabilityDensity(in GeometryPoint point, in Float3 incident) => scene.ProbabilityDensity(token, point, incident);

	/// <summary>
	/// Evaluates the emissive contribution of <paramref name="point"/> with <paramref name="material"/>
	/// towards <paramref name="outgoing"/>. Only invoke this method if <see cref="Material.IsEmissive"/>!
	/// </summary>
	public static Float3 Evaluate(in GeometryPoint point, in Float3 outgoing, Material material)
	{
		Assert.IsTrue(material.IsEmissive);
		return Evaluate(point, -outgoing, material.Emission);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static Float3 Evaluate(in GeometryPoint point, in Float3 incident, in Float3 emission) => incident.Dot(point.normal) > 0f ? Float3.zero : emission;
}