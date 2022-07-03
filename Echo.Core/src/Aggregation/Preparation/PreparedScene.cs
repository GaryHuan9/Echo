using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Acceleration;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Scenic;
using Echo.Core.Scenic.Geometric;
using Echo.Core.Scenic.Lighting;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Aggregation.Preparation;

/// <summary>
/// A <see cref="Scene"/> prepared ready for fast interactions.
/// </summary>
public class PreparedScene : PreparedPack
{
	public PreparedScene(ReadOnlySpan<IGeometrySource> geometrySources, ReadOnlySpan<ILightSource> lightSources,
						 ImmutableArray<PreparedInstance> instances, in AcceleratorCreator acceleratorCreator,
						 SwatchExtractor swatchExtractor, IEnumerable<InfiniteLight> infiniteLights, Camera camera)
		: base(geometrySources, lightSources, instances, in acceleratorCreator, swatchExtractor)
	{
		this.camera = camera;
		rootInstance = new PreparedInstance(this, geometries.swatch, Float4x4.identity);
		this.infiniteLights = FilterLights(infiniteLights, rootInstance);
		infiniteLightsThreshold = CalculateThreshold(this.infiniteLights, rootInstance.Power);
		infiniteLightsPdf = infiniteLightsThreshold / this.infiniteLights.Length;
	}

	public readonly Camera camera;
	public readonly ImmutableArray<InfiniteLight> infiniteLights;

	readonly PreparedInstance rootInstance;
	readonly float infiniteLightsThreshold;
	readonly float infiniteLightsPdf;

	/// <summary>
	/// Processes the <paramref name="query"/> and returns whether it intersected with something.
	/// </summary>
	public bool Trace(ref TraceQuery query)
	{
		Assert.AreEqual(query.current, new TokenHierarchy());
		if (!FastMath.Positive(query.distance)) return false;
		float original = query.distance;

		accelerator.Trace(ref query);
		return query.distance < original;
	}

	/// <summary>
	/// Processes the <paramref name="query"/> and returns whether it is occluded by something.
	/// </summary>
	public bool Occlude(ref OccludeQuery query)
	{
		Assert.AreEqual(query.current, new TokenHierarchy());
		if (!FastMath.Positive(query.travel)) return false;
		return accelerator.Occlude(ref query);
	}

	/// <summary>
	/// Interacts with a concluded <see cref="TraceQuery"/> that was performed
	/// on this <see cref="PreparedScene"/> by creating a <see cref="Contact"/>.
	/// </summary>
	public Contact Interact(in TraceQuery query)
	{
		query.AssertHit();

		ref readonly var instance = ref FindLayer(query.token, out _, out Float4x4 inverseTransform);
		Contact.Info info = instance.pack.geometries.GetContactInfo(query.token.TopToken, query.uv);

		Float3 normal = inverseTransform.MultiplyDirection(info.normal).Normalized;
		return new Contact(query, normal, instance.swatch[info.material], info.texcoord);
	}

	public Probable<TokenHierarchy> Pick(in GeometryPoint origin, Sample1D sample)
	{
		if (sample < infiniteLightsThreshold)
		{
			sample = sample.Stretch(0f, infiniteLightsThreshold);
			int index = sample.Range(infiniteLights.Length);
			var token = new EntityToken(LightType.Infinite, index);

			return (new TokenHierarchy { TopToken = token }, infiniteLightsPdf);
		}

		sample = sample.Stretch(infiniteLightsThreshold, 1f);

		PreparedPack pack = this;
		var hierarchy = new TokenHierarchy();
		float pdf = 1f - infiniteLightsThreshold;

		while (true)
		{
			(EntityToken token, float tokenPdf) = pack.lightPicker.Pick(origin, ref sample);

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

	public float ProbabilityMass(in GeometryPoint origin, in TokenHierarchy light)
	{
		if (light.TopToken.IsInfiniteLight())
		{
			Assert.AreEqual(light.InstanceCount, 0);
			return infiniteLightsPdf;
		}

		PreparedPack pack = this;
		float pdf = 1f;

		foreach (EntityToken token in light.Instances)
		{
			Assert.AreEqual(token.Type, TokenType.Instance);
			pdf *= pack.lightPicker.ProbabilityMass(origin, token);
			pack = pack.geometries.instances.ItemRef(token.Index).pack;
		}

		return pdf * pack.lightPicker.ProbabilityMass(origin, light.TopToken);
	}

	public Probable<RGB128> Sample(in TokenHierarchy light, in GeometryPoint origin, Sample2D sample, out Float3 incident, out float travel)
	{
		EntityToken token = light.TopToken;

		if (token.IsInfiniteLight())
		{
			InfiniteLight infiniteLight = infiniteLights[token.Index];
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

	public float ProbabilityDensity(in TokenHierarchy light, in GeometryPoint origin, in Float3 incident)
	{
		EntityToken token = light.TopToken;

		if (token.IsInfiniteLight())
		{
			InfiniteLight infiniteLight = infiniteLights[token.Index];
			return infiniteLight.ProbabilityDensity(origin, incident);
		}

		ref readonly var instance = ref FindLayer(light, out Float4x4 forwardTransform, out Float4x4 inverseTransform);

		return instance.pack.lights.ProbabilityDensity
		(
			token, forwardTransform * origin,
			inverseTransform.MultiplyDirection(incident).Normalized
		);
	}

	/// <summary>
	/// Evaluates all of the <see cref="AmbientLight"/> in this <see cref="PreparedScene"/>.
	/// </summary>
	/// <param name="direction">The normalized direction in world-space.</param>
	/// <returns>The evaluated <see cref="RGB128"/> light value.</returns>
	public RGB128 EvaluateInfinite(Float3 direction)
	{
		Assert.AreEqual(direction.SquaredMagnitude, 1f);

		var total = RGB128.Black;

		foreach (var light in infiniteLights) total += light.Evaluate(direction);

		return total;
	}

	ref readonly PreparedInstance FindLayer(in TokenHierarchy hierarchy, out Float4x4 forwardTransform, out Float4x4 inverseTransform)
	{
		forwardTransform = Float4x4.identity;
		inverseTransform = Float4x4.identity;

		ref readonly PreparedInstance instance = ref rootInstance;

		//Check if we can exit early
		if (hierarchy.InstanceCount == 0) return ref instance;

		//Traverse down the instancing hierarchy
		foreach (ref readonly EntityToken token in hierarchy.Instances)
		{
			Assert.AreEqual(token.Type, TokenType.Instance);
			instance = ref instance.pack.geometries.instances.ItemRef(token.Index);

			//Because we traverse in reverse, we must also multiply the transform in reverse
			forwardTransform = instance.forwardTransform * forwardTransform;
			inverseTransform = instance.inverseTransform * inverseTransform;
		}

		return ref instance;
	}

	static ImmutableArray<InfiniteLight> FilterLights(IEnumerable<InfiniteLight> lights, in PreparedInstance rootInstance)
	{
		var builder = ImmutableArray.CreateBuilder<InfiniteLight>();

		foreach (InfiniteLight light in lights)
		{
			light.Prepare((PreparedScene)rootInstance.pack);
			if (!FastMath.Positive(light.Power)) continue;

			builder.Add(light);
		}

		if (builder.Count == 0 && !FastMath.Positive(rootInstance.Power))
		{
			//Degenerate case with literally zero light contributor; our output image will literally be
			//completely black, but we add in a light so no exception is thrown when we look for lights.

			var light = new AmbientLight { Intensity = RGB128.Black };
			light.Prepare((PreparedScene)rootInstance.pack);

			builder.Capacity = 1;
			builder.Add(light);

			return builder.MoveToImmutable();
		}

		return builder.ToImmutableArray();
	}

	static float CalculateThreshold(ImmutableArray<InfiniteLight> infiniteLights, float instancePower)
	{
		const float InstanceBias = Scalars.Tau;

		float infiniteLightsPower = infiniteLights.Sum(light => light.Power);
		float sum = infiniteLightsPower + instancePower * InstanceBias;
		return FastMath.Positive(sum) ? infiniteLightsPower / sum : 1f;
	}
}