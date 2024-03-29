﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Echo.Core.Aggregation.Acceleration;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Scenic.Cameras;
using Echo.Core.Scenic.Geometries;
using Echo.Core.Scenic.Hierarchies;
using Echo.Core.Scenic.Lights;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Aggregation.Preparation;

/// <summary>
/// A <see cref="Scene"/> prepared for fast interactions.
/// </summary>
public sealed class PreparedScene : PreparedPack
{
	public PreparedScene(ReadOnlySpan<IGeometrySource> geometrySources, ReadOnlySpan<ILightSource> lightSources,
						 ImmutableArray<PreparedInstance> instances, in AcceleratorCreator acceleratorCreator,
						 SwatchExtractor swatchExtractor, IEnumerable<InfiniteLight> infiniteLights, Camera camera)
		: base(geometrySources, lightSources, instances, acceleratorCreator, swatchExtractor)
	{
		this.camera = camera;
		if (camera == null) throw new ArgumentNullException(nameof(camera));

		rootInstance = new PreparedInstance(this, geometries.swatch, Float4x4.Identity);
		this.infiniteLights = FilterLights(infiniteLights, rootInstance);
		infiniteLightsPower = SumInfiniteLightsPower(this.infiniteLights);

		infiniteLightsThreshold = CalculateThreshold(infiniteLightsPower, rootInstance.Power);
		infiniteLightsPdf = infiniteLightsThreshold / this.infiniteLights.Length;
	}

	/// <summary>
	/// The sensor that captures and evaluates this <see cref="PreparedScene"/>.
	/// </summary>
	public readonly Camera camera;

	/// <summary>
	/// All of the <see cref="InfiniteLight"/> in this <see cref="PreparedScene"/>.
	/// </summary>
	public readonly ImmutableArray<InfiniteLight> infiniteLights;

	/// <summary>
	/// The total <see cref="InfiniteLight.Power"/> of <see cref="infiniteLights"/>.
	/// </summary>
	public readonly float infiniteLightsPower;

	readonly PreparedInstance rootInstance;
	readonly float infiniteLightsThreshold;
	readonly float infiniteLightsPdf;

	/// <summary>
	/// Processes the <see cref="TraceQuery"/>.
	/// </summary>
	/// <returns>Whether the <see cref="TraceQuery"/> intersected with something.</returns>
	/// <seealso cref="Accelerator.Trace"/>
	public bool Trace(ref TraceQuery query)
	{
		Ensure.AreEqual(query.current, new TokenHierarchy());
		if (!FastMath.Positive(query.distance)) return false;
		float original = query.distance;

		accelerator.Trace(ref query);
		return query.distance < original;
	}

	/// <summary>
	/// Processes the <see cref="OccludeQuery"/>.
	/// </summary>
	/// <returns>Whether the <see cref="OccludeQuery"/> is occluded by something.</returns>
	/// <seealso cref="Accelerator.Occlude"/>
	public bool Occlude(ref OccludeQuery query)
	{
		Ensure.AreEqual(query.current, new TokenHierarchy());
		if (!FastMath.Positive(query.travel)) return false;
		return accelerator.Occlude(ref query);
	}

	/// <summary>
	/// Interacts with a surface of this <see cref="PreparedScene"/>.
	/// </summary>
	/// <param name="query">A successfully concluded <see cref="TraceQuery"/>
	/// performed on this <see cref="PreparedPack"/>.</param>
	/// <returns>A <see cref="Contact"/> value containing the necessary information
	/// about this interaction with the <see cref="PreparedScene"/>.</returns>
	public Contact Interact(in TraceQuery query)
	{
		query.EnsureHit();

		ref readonly var instance = ref FindLayer(query.token, out _, out Float4x4 inverseTransform);
		Contact.Info info = instance.pack.geometries.GetContactInfo(query.token.TopToken, query.uv);

		Float3 normal = inverseTransform.MultiplyDirection(info.normal).Normalized;
		Float3 shadingNormal = inverseTransform.MultiplyDirection(info.shadingNormal).Normalized;
		return new Contact(query, normal, instance.swatch[info.material], shadingNormal, info.texcoord);
	}

	/// <summary>
	/// Selects a light in this <see cref="PreparedScene"/>.
	/// </summary>
	/// <param name="origin">The <see cref="GeometryPoint"/> from which this light should be selected based off of.</param>
	/// <param name="sample">The <see cref="Sample1D"/> value used for this selection.</param>
	/// <returns>The selected light, packaged into a <see cref="TokenHierarchy"/>.</returns>
	public Probable<TokenHierarchy> Pick(in GeometryPoint origin, Sample1D sample)
	{
		if (sample < infiniteLightsThreshold)
		{
			sample = sample.Stretch(0f, infiniteLightsThreshold);
			int index = sample.Range(infiniteLights.Length);

			var lightType = infiniteLights[index].IsDelta ? LightType.InfiniteDelta : LightType.Infinite;
			return (new TokenHierarchy { TopToken = new EntityToken(lightType, index) }, infiniteLightsPdf);
		}

		sample = sample.Stretch(infiniteLightsThreshold, 1f);

		PreparedPack pack = this;
		var hierarchy = new TokenHierarchy();
		float pdf = 1f - infiniteLightsThreshold;

		while (true)
		{
			(EntityToken token, float tokenPdf) = pack.lightPicker.Pick(origin, ref sample);
			if (FastMath.AlmostZero(tokenPdf)) return Probable<TokenHierarchy>.Impossible;

			pdf *= tokenPdf;

			if (token.Type != TokenType.Instance)
			{
				hierarchy.TopToken = token;
				break;
			}

			ref readonly var instance = ref pack.geometries.instances.ItemRef(token.Index);

			hierarchy.Push(token);
			pack = instance.pack;
		}

		return new Probable<TokenHierarchy>(hierarchy, pdf);
	}

	/// <summary>
	/// Returns the probability mass function (pmf) value of selecting a light in this <see cref="PreparedScene"/>.
	/// </summary>
	/// <param name="light">A <see cref="TokenHierarchy"/> that represents the light that was selected.</param>
	/// <param name="origin">The <see cref="GeometryPoint"/> from which the selection was based off of.</param>
	/// <returns>The calculated pmf value.</returns>
	public float ProbabilityMass(in TokenHierarchy light, in GeometryPoint origin)
	{
		if (light.TopToken.IsInfiniteLight())
		{
			Ensure.AreEqual(light.InstanceCount, 0);
			return infiniteLightsPdf;
		}

		PreparedPack pack = this;
		float pdf = 1f - infiniteLightsThreshold;

		foreach (EntityToken token in light.Instances)
		{
			Ensure.AreEqual(token.Type, TokenType.Instance);
			pdf *= pack.lightPicker.ProbabilityMass(token, origin);
			pack = pack.geometries.instances.ItemRef(token.Index).pack;

			if (FastMath.AlmostZero(pdf)) return 0f;
		}

		return pdf * pack.lightPicker.ProbabilityMass(light.TopToken, origin);
	}

	/// <inheritdoc cref="IPreparedLight.Sample"/>
	public Probable<RGB128> Sample(in TokenHierarchy light, in GeometryPoint origin, Sample2D sample, out Float3 incident, out float travel)
	{
		EntityToken token = light.TopToken;

		if (token.IsInfiniteLight())
		{
			var infiniteLight = infiniteLights[token.LightIndex];
			return infiniteLight.Sample(origin, sample, out incident, out travel);
		}

		ref readonly var instance = ref FindLayer(light, out Float4x4 forwardTransform, out Float4x4 inverseTransform);

		Probable<RGB128> result = instance.pack.lights.Sample
		(
			token, forwardTransform * origin,
			sample, out incident, out travel
		);

		incident = inverseTransform.MultiplyDirection(incident).Normalized;
		travel *= Utility.GetScale(inverseTransform);

		return result;
	}

	/// <inheritdoc cref="IPreparedLight.ProbabilityDensity"/>
	public float ProbabilityDensity(in TokenHierarchy light, in GeometryPoint origin, Float3 incident)
	{
		EntityToken token = light.TopToken;

		if (token.IsInfiniteLight())
		{
			var infiniteLight = infiniteLights[token.LightIndex];
			return infiniteLight.ProbabilityDensity(origin, incident);
		}

		ref readonly var instance = ref FindLayer(light, out Float4x4 forwardTransform, out _);

		return instance.pack.lights.ProbabilityDensity
		(
			token, forwardTransform * origin,
			forwardTransform.MultiplyDirection(incident).Normalized
		);
	}

	/// <summary>
	/// Evaluates all of the <see cref="InfiniteLight"/> in this <see cref="PreparedScene"/>.
	/// </summary>
	/// <param name="direction">The normalized direction in world-space.</param>
	/// <param name="direct">Whether this is a direct evaluation, which may hide some
	/// <see cref="InfiniteLight"/> based on <see cref="InfiniteLight.DirectlyVisible"/>.</param>
	/// <returns>The evaluated <see cref="RGB128"/> light value.</returns>
	public RGB128 EvaluateInfinite(Float3 direction, bool direct = false)
	{
		Ensure.AreEqual(direction.SquaredMagnitude, 1f);

		var total = RGB128.Black;

		if (direct)
		{
			foreach (var light in infiniteLights)
			{
				if (!light.DirectlyVisible) continue;
				total += light.Evaluate(direction);
			}
		}
		else
		{
			foreach (var light in infiniteLights) total += light.Evaluate(direction);
		}

		return total;
	}

	ref readonly PreparedInstance FindLayer(in TokenHierarchy hierarchy, out Float4x4 forwardTransform, out Float4x4 inverseTransform)
	{
		forwardTransform = Float4x4.Identity;
		inverseTransform = Float4x4.Identity;

		ref readonly PreparedInstance instance = ref rootInstance;

		//Check if we can exit early
		if (hierarchy.InstanceCount == 0) return ref instance;

		//Traverse down the instancing hierarchy
		foreach (ref readonly EntityToken token in hierarchy.Instances)
		{
			Ensure.AreEqual(token.Type, TokenType.Instance);
			instance = ref instance.pack.geometries.instances.ItemRef(token.Index);

			//Because we traverse in reverse, we must also multiply the transform in reverse
			forwardTransform = instance.forwardTransform * forwardTransform;
			inverseTransform = instance.inverseTransform * inverseTransform;
		}

		return ref instance;
	}

	static ImmutableArray<InfiniteLight> FilterLights(IEnumerable<InfiniteLight> infiniteLights, in PreparedInstance rootInstance)
	{
		var builder = ImmutableArray.CreateBuilder<InfiniteLight>();

		foreach (InfiniteLight light in infiniteLights)
		{
			light.Prepare((PreparedScene)rootInstance.pack);
			if (!FastMath.Positive(light.Power)) continue;

			builder.Add(light);
		}

		if (builder.Count == 0 && !FastMath.Positive(rootInstance.Power))
		{
			//Degenerate case with literally zero light contributor; our output image will be
			//pure black, but we add in a light so no exception is thrown when we look for lights.

			var light = new AmbientLight { Intensity = RGB128.Black };
			light.Prepare((PreparedScene)rootInstance.pack);

			builder.Capacity = 1;
			builder.Add(light);

			return builder.MoveToImmutable();
		}

		return builder.ToImmutableArray();
	}

	static float SumInfiniteLightsPower(ImmutableArray<InfiniteLight> infiniteLights)
	{
		float power = 0f;

		foreach (InfiniteLight light in infiniteLights) power += light.Power;

		return power;
	}

	static float CalculateThreshold(float infiniteLightsPower, float instancePower)
	{
		//We induce a multiplier to favor sampling the instance lights over the infinite lights because infinite
		//lights can easily dominate over the scene and they are specially sampled when a ray escapes the scene

		const float InstanceBias = Scalars.Phi * Scalars.Phi;
		float sum = infiniteLightsPower + instancePower * InstanceBias;
		return FastMath.Positive(sum) ? infiniteLightsPower / sum : 1f;
	}
}