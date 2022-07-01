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
						 SwatchExtractor swatchExtractor, IEnumerable<InfiniteLight> infiniteLights)
		: base(geometrySources, lightSources, instances, in acceleratorCreator, swatchExtractor)
	{
		rootInstance = new PreparedInstance(this, geometries.swatch, Float4x4.identity);
		(this.infiniteLights, ambientLights) = FilterLights(infiniteLights, rootInstance);
		infiniteLightsChance = CalculateLightChance(this.infiniteLights, rootInstance.Power);
	}

	public readonly ImmutableArray<InfiniteLight> infiniteLights;
	public readonly ImmutableArray<AmbientLight> ambientLights;

	readonly PreparedInstance rootInstance;
	readonly float infiniteLightsChance;

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

	public Probable<TokenHierarchy> Pick(Sample1D sample)
	{
		var hierarchy = new TokenHierarchy();
		float pdf;

		if (sample < infiniteLightsChance)
		{
			sample = sample.Stretch(0f, infiniteLightsChance);
			int index = sample.Range(infiniteLights.Length);

			hierarchy.TopToken = new EntityToken(LightType.Infinite, index);
			pdf = infiniteLightsChance / infiniteLights.Length;
		}
		else
		{
			sample = sample.Stretch(infiniteLightsChance, 1f);

			pdf = 1f;

			PreparedPack pack = this;

			while (true)
			{
				(EntityToken token, float tokenPdf) = pack.lightPicker.Pick(sample);

				pdf *= tokenPdf;

				if (token.Type != TokenType.Instance)
				{
					hierarchy.TopToken = token;
					break;
				}

				hierarchy.Push(token);
				pack = pack.geometries.instances[token.Index].pack;
				sample = sample.Stretch(/*TODO*/);
			}
		}

		return new Probable<TokenHierarchy>(hierarchy, pdf);
	}

	public float ProbabilityMass(in TokenHierarchy light)
	{
		throw new NotImplementedException();
	}

	public Probable<RGB128> Sample(in TokenHierarchy light, in GeometryPoint origin, Sample2D sample, out Float3 incident, out float travel)
	{
		ref readonly var instance = ref FindLayer(light, out Float4x4 forwardTransform, out Float4x4 inverseTransform);

		Probable<RGB128> result = instance.pack.lights.Sample
		(
			light.TopToken, forwardTransform * origin,
			sample, out incident, out travel
		);

		incident = inverseTransform.MultiplyDirection(incident).Normalized;
		travel *= Utility.GetScale(inverseTransform);

		return result;
	}

	public float ProbabilityDensity(in TokenHierarchy light, in GeometryPoint origin, in Float3 incident)
	{
		ref readonly var instance = ref FindLayer(light, out Float4x4 forwardTransform, out Float4x4 inverseTransform);

		return instance.pack.lights.ProbabilityDensity
		(
			light.TopToken, forwardTransform * origin,
			inverseTransform.MultiplyDirection(incident).Normalized
		);
	}

	/// <summary>
	/// Evaluates all of the <see cref="AmbientLight"/> in this <see cref="PreparedScene"/>.
	/// </summary>
	/// <param name="direction">The normalized direction in world-space.</param>
	/// <returns>The evaluated <see cref="RGB128"/> light value.</returns>
	public RGB128 EvaluateAmbient(Float3 direction)
	{
		Assert.AreEqual(direction.SquaredMagnitude, 1f);

		var total = RGB128.Black;

		foreach (AmbientLight light in ambientLights) total += light.Evaluate(direction);

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

	static (ImmutableArray<InfiniteLight>, ImmutableArray<AmbientLight>) FilterLights(IEnumerable<InfiniteLight> lights, in PreparedInstance rootInstance)
	{
		var infiniteLights = ImmutableArray.CreateBuilder<InfiniteLight>();
		var ambientLights = ImmutableArray.CreateBuilder<AmbientLight>();

		foreach (InfiniteLight light in lights)
		{
			light.Prepare((PreparedScene)rootInstance.pack);
			if (!FastMath.Positive(light.Power)) continue;

			infiniteLights.Add(light);
			if (light is AmbientLight ambient) ambientLights.Add(ambient);
		}

		if (infiniteLights.Count == 0 && !FastMath.Positive(rootInstance.Power))
		{
			//Degenerate case with literally zero light contributor; our output image will literally be
			//completely black, but we add in a light so no exception is thrown when we look for lights.

			var light = new AmbientLight { Intensity = RGB128.Black };
			light.Prepare((PreparedScene)rootInstance.pack);

			ambientLights.Capacity = 1;
			ambientLights.Add(light);

			var result = ambientLights.MoveToImmutable();
			return (result.CastArray<InfiniteLight>(), result);
		}

		return (infiniteLights.ToImmutableArray(), ambientLights.ToImmutable());
	}

	static float CalculateLightChance(ImmutableArray<InfiniteLight> infiniteLights, float instancePower)
	{
		const float InstanceMultiplier = Scalars.Tau;

		float infiniteLightsPower = infiniteLights.Sum(light => light.Power);
		float sum = infiniteLightsPower + instancePower * InstanceMultiplier;
		return FastMath.Positive(sum) ? infiniteLightsPower / sum : 1f;
	}
}