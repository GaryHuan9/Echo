using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Evaluation.Distributions.Continuous;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Scenic.Lighting;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Evaluation.Evaluators;

public record PathTracedEvaluator : Evaluator
{
	/// <summary>
	/// The maximum number of bounces a path can have before it is immediately terminated unconditionally.
	/// If such occurrence appears, the sample becomes biased and this property should be increased.
	/// </summary>
	public int BounceLimit { get; init; } = 128;

	/// <summary>
	/// The survivability of a path during unbiased path termination. As this value goes up, the amount of variance decreases, and 
	/// the time we spend on each path increases. This value should be relatively high for interior scenes while low for outdoors.
	/// </summary>
	public float Survivability { get; init; } = 2.5f;

	public override IEvaluationLayer CreateOrClearLayer(RenderBuffer buffer) => CreateOrClearLayer<RGB128>(buffer, "path");

	[SkipLocalsInit]
	public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator)
	{
		var path = new Path(ray);

		//Quick exit with ambient light if no intersection
		if (!path.Advance(scene, allocator)) return scene.EvaluateInfinite(path.CurrentDirection);

		//Add emission for first hit, if available
		path.ContributeEmissive();

		for (int depth = 0; depth < BounceLimit; depth++)
		{
			//Sample the bsdf
			var bounce = new Bounce(path.contact, distribution.Next2D());
			if (bounce.IsZero) break;

			//Prefetch all samples so the order of them in subsequent bounces do not get messed up
			Sample1D lightSample = distribution.Next1D();
			Sample1D survivalSample = distribution.Next1D();
			Sample2D radiantSample = distribution.Next2D();

			//If the bounce is specular, then we do not use multiple importance sampling (MIS)
			if (bounce.IsSpecular)
			{
				//Check if the path is exhausted
				if (!path.Continue(bounce, Survivability, survivalSample)) break;

				//Begin finding a new bounce
				if (!path.Advance(scene, allocator))
				{
					//None found, accumulate ambient and exit
					path.Contribute(scene.EvaluateInfinite(path.CurrentDirection));
					break;
				}

				//Try add intersected emission
				path.ContributeEmissive();
			}
			else
			{
				//Importance sample scene radiance
				path.Contribute(ImportanceSampleRadiant
				(
					scene, path.contact, lightSample,
					radiantSample, out bool mis
				));

				if (mis)
				{
					//We have both an area light a non-delta BSDF, begin MIS
					//Continue path to the next vertex and exit if path exhausted
					if (!path.Continue(bounce, Survivability, survivalSample)) break;

					GeometryPoint oldOrigin = path.contact.point;

					//Advance path and perform MIS
					if (path.Advance(scene, allocator))
					{
						//Attempt to do MIS on the newly contacted surface
						ref readonly var light = ref path.contact.token;
						float pmf = scene.ProbabilityMass(oldOrigin, light);
						if (!FastMath.Positive(pmf)) goto noLight;

						float pdf = pmf * scene.ProbabilityDensity(light, oldOrigin, path.CurrentDirection);
						if (!FastMath.Positive(pdf)) goto noLight;

						float weight = PowerHeuristic(bounce.scatterPdf, pdf);
						path.ContributeEmissive(weight / pdf);

					noLight:
						{ }
					}
					else
					{
						//Use infinite lights for MIS if there is no intersection
						var hierarchy = new TokenHierarchy();
						Float3 direction = path.CurrentDirection;

						for (int i = 0; i < scene.infiniteLights.Length; i++)
						{
							InfiniteLight light = scene.infiniteLights[i];
							hierarchy.TopToken = new EntityToken(LightType.Infinite, i);

							float pdf = scene.ProbabilityMass(oldOrigin, hierarchy) *
										light.ProbabilityDensity(oldOrigin, direction);
							if (!FastMath.Positive(pdf)) continue;

							float weight = PowerHeuristic(bounce.scatterPdf, pdf);
							path.Contribute(light.Evaluate(direction) / pdf * weight);
						}

						break;
					}
				}
				else
				{
					//Our light does not like MIS either, so no MIS will be performed
					//Begin continue path to the next vertex and exit if path exhausted
					if (!path.Continue(bounce, Survivability, survivalSample)) break;

					//Exit if no intersection with the scene
					if (!path.Advance(scene, allocator)) break;

					//TODO: Add infinite lights??
				}
			}
		}

		return path.Result;
	}

	/// <summary>
	/// Importance samples a the radiant in a <see cref="PreparedScene"/>.
	/// </summary>
	/// <param name="scene">The <see cref="PreparedScene"/> that all of this takes place.</param>
	/// <param name="contact">The <see cref="Contact"/> to sample from.</param>
	/// <param name="lightSample">The <see cref="Sample1D"/> value to use to select a light.</param>
	/// <param name="radiantSample">The <see cref="Sample2D"/> value to use to sample the light.</param>
	/// <param name="mis">Whether this method used multiple importance sampling.</param>
	/// <returns>The sampled radiant from the <see cref="PreparedScene"/>.</returns>
	static RGB128 ImportanceSampleRadiant(PreparedScene scene, in Contact contact, Sample1D lightSample, Sample2D radiantSample, out bool mis)
	{
		//Select light from scene and sample it
		(TokenHierarchy light, float lightPdf) = scene.Pick(contact.point, lightSample);
		(RGB128 radiant, float radiantPdf) = scene.Sample
		(
			light, contact.point, radiantSample,
			out Float3 incident, out float travel
		);

		float pdf = lightPdf * radiantPdf;
		mis = light.TopToken.IsAreaLight();

		if (!FastMath.Positive(pdf) | radiant.IsZero) return RGB128.Black;

		//Evaluate bsdf at the direction sampled for our light
		ref readonly Float3 outgoing = ref contact.outgoing;
		RGB128 scatter = contact.bsdf.Evaluate(outgoing, incident);
		scatter *= contact.NormalDot(incident);

		//Conditionally terminate if radiant cannot be positive
		if (scatter.IsZero) return RGB128.Black;
		var query = contact.SpawnOcclude(incident, travel);
		if (scene.Occlude(ref query)) return RGB128.Black;

		//Calculate final radiant
		radiant /= pdf;
		if (!mis) return scatter * radiant;

		float weight = PowerHeuristic(pdf, contact.bsdf.ProbabilityDensity(outgoing, incident));
		return radiant * weight * scatter;
	}

	/// <summary>
	/// Power heuristic with a constant power of two used for multiple importance sampling.
	/// NOTE: <paramref name="pdf0"/> will become the numerator, not <paramref name="pdf1"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static float PowerHeuristic(float pdf0, float pdf1)
	{
		float squared = pdf0 * pdf0;
		return squared / (squared + pdf1 * pdf1);
	}

	/// <summary>
	/// The path of a photon within a <see cref="PreparedScene"/>. 
	/// </summary>
	[StructLayout(LayoutKind.Auto)]
	struct Path
	{
		[SkipLocalsInit]
		public Path(in Ray ray)
		{
			Result = RGB128.Black;
			energy = RGB128.White;
			Unsafe.SkipInit(out contact);
			query = new TraceQuery(ray);
		}

		/// <summary>
		/// The accumulating result of this <see cref="Path"/>.
		/// </summary>
		public RGB128 Result { get; private set; }

		RGB128 energy;

		/// <summary>
		/// The current <see cref="Contact"/> experienced on this <see cref="Path"/>.
		/// </summary>
		public Contact contact;

		TraceQuery query;

		/// <summary>
		/// The current <see cref="Float3"/> direction of this <see cref="Path"/>.
		/// </summary>
		public readonly Float3 CurrentDirection => query.ray.direction;

		readonly Material Material => contact.shade.material;

		/// <summary>
		/// Advance this <see cref="Path"/> to a new vertex.
		/// </summary>
		/// <param name="scene">The <see cref="PreparedScene"/> that this <see cref="Path"/> exists in.</param>
		/// <param name="allocator">The <see cref="Allocator"/> to use.</param>
		/// <returns>Whether a new vertex was successfully created or this <see cref="Path"/> escaped the <see cref="PreparedScene"/>.</returns>
		public bool Advance(PreparedScene scene, Allocator allocator)
		{
			allocator.Restart();

			while (scene.Trace(ref query))
			{
				contact = scene.Interact(query);

				Material.Scatter(ref contact, allocator);
				if (contact.bsdf != null) return true;

				query = query.SpawnTrace();
			}

			return false;
		}

		/// <summary>
		/// Completes this <see cref="Path"/> after a <see cref="Bounce"/>.
		/// </summary>
		/// <param name="bounce">The <see cref="Bounce"/> of which this <see cref="Path"/> just completed.</param>
		/// <param name="survivability">The likeliness of this <see cref="Path"/> to continue after this <see cref="Bounce"/>.</param>
		/// <param name="sample">The <see cref="Sample1D"/> value used during this operation.</param>
		/// <returns>True if this <see cref="Path"/> survived and it should continue tracing,
		/// or false if the <see cref="Path"/> is exhausted and should be terminated.</returns>
		/// <seealso cref="Survivability"/>
		public bool Continue(in Bounce bounce, float survivability, Sample1D sample)
		{
			energy *= bounce.scatter / bounce.scatterPdf;

			//Conditional path termination with Russian Roulette
			bool survived = RussianRoulette(ref energy, survivability, sample);
			if (survived) query = query.SpawnTrace(bounce.incident);

			return survived;
		}

		/// <summary>
		/// Contributes to the <see cref="Result"/> of this <see cref="Path"/>.
		/// </summary>
		/// <param name="value">The <see cref="RGB128"/> value to contribute.</param>
		public void Contribute(in RGB128 value) => Result += energy * value; //OPTIMIZE: fma

		/// <summary>
		/// Contributes the emission of the <see cref="Material"/> of the current <see cref="contact"/>.
		/// </summary>
		/// <param name="weight">An optionally provided value to scale this contribution.</param>
		public void ContributeEmissive(float weight = 1f)
		{
			if (Material is not IEmissive emissive) return;
			if (!FastMath.Positive(emissive.Power)) return;

			Contribute(emissive.Emit(contact.point, contact.outgoing) * weight);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool RussianRoulette(ref RGB128 energy, float survivability, Sample1D sample)
		{
			float rate = FastMath.Clamp01(survivability * energy.Luminance);

			if (sample >= rate) return false;

			energy /= rate;
			return true;
		}
	}

	/// <summary>
	/// A scattering event occured when a <see cref="Path"/> is traced.
	/// </summary>
	readonly struct Bounce
	{
		public Bounce(in Contact contact, Sample2D sample)
		{
			(scatter, scatterPdf) = contact.bsdf.Sample
			(
				contact.outgoing, sample, out incident,
				out function, TryExcludeSpecular(contact.bsdf)
			);

			scatter *= contact.NormalDot(incident);
		}

		/// <summary>
		/// The scattered color value of this <see cref="Bounce"/>.
		/// </summary>
		public readonly RGB128 scatter;

		/// <summary>
		/// The incident normal direction at this <see cref="Bounce"/>.
		/// </summary>
		public readonly Float3 incident;

		/// <summary>
		/// The probability density function (pdf) of the <see cref="scatter"/>.
		/// </summary>
		public readonly float scatterPdf;

		readonly BxDF function;

		/// <summary>
		/// Whether this <see cref="Bounce"/> will have zero contribution to the <see cref="Path"/>.
		/// </summary>
		public bool IsZero => !FastMath.Positive(scatterPdf) | scatter.IsZero;

		/// <summary>
		/// Whether the <see cref="Marshal"/> that this <see cref="Bounce"/> occured on is specular.
		/// </summary>
		public bool IsSpecular => function.type.Any(FunctionType.Specular);

		/// <summary>
		/// Returns a <see cref="FunctionType"/> that tries to exclude all <see cref="BxDF"/> of type <see cref="FunctionType.Specular"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static FunctionType TryExcludeSpecular(BSDF bsdf)
		{
			int count = bsdf.Count(FunctionType.Specular);
			int total = bsdf.Count(FunctionType.All);

			return count == 0 || count == total ? FunctionType.All : ~FunctionType.Specular;
		}
	}
}