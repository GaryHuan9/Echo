﻿using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
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

	PreparedScene scene;
	GeometryToken token;
	Float3 emission;

	/// <inheritdoc/>
	public Float3 Sample(in GeometryPoint point, Sample2D sample, out Float3 incident, out float pdf, out float travel)
	{
		GeometryPoint sampled = scene.Sample(token, point, sample, out pdf);

		incident = sampled.position - point;

		travel = incident.Magnitude;
		incident *= 1f / travel;

		return incident.Dot(sampled.normal) > 0f ? Float3.zero : emission;
	}

	/// <inheritdoc/>
	public float ProbabilityDensity(in GeometryPoint point, in Float3 incident) => scene.ProbabilityDensity(token, point, incident);
}