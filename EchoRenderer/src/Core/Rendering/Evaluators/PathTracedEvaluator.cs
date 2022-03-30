using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Distributions;
using EchoRenderer.Core.Rendering.Distributions.Continuous;
using EchoRenderer.Core.Rendering.Materials;
using EchoRenderer.Core.Rendering.Scattering;
using EchoRenderer.Core.Scenic.Lights;
using EchoRenderer.Core.Scenic.Preparation;

namespace EchoRenderer.Core.Rendering.Evaluators;

public class PathTracedEvaluator : Evaluator
{
	/// <summary>
	/// The maximum number of bounces a path can have before it is immediately terminated unconditionally.
	/// If such occurrence appears, the sample becomes biased and this property should be increased.
	/// </summary>
	public int BounceLimit { get; set; } = 128;

	/// <summary>
	/// The survivability of a path with during unbiased path termination. As this value goes up, the amount of variance decreases,
	/// and the time spend on each path increases. This value should be relatively high for interior scenes while low for outdoors.
	/// </summary>
	public float Survivability { get; set; } = 2.5f;

	//NOTE: although the word 'radiant' is frequently used here to denote particles of energy accumulating,
	//technically it is not correct because the size of the emitter could be either a point or an area,
	//(so for the same reason the word 'radiance' is also wrong) we just chose 'radiant' because it has the
	//same length as the word 'scatter'.

	[SkipLocalsInit]
	public override Float3 Evaluate(in Ray ray, RenderProfile profile, Arena arena)
	{
		Allocator allocator = arena.allocator;
		var distribution = arena.Distribution;

		var scene = profile.Scene;
		var path = new Path(ray);

		//Quick exit with ambient light if no intersection
		if (!path.FindNext(scene, allocator)) return EvaluateAllAmbient();

		//Allocate memory for samples used for lights
		Span<Sample1D> lightSamples = stackalloc Sample1D[scene.info.depth + 1];

		//Add emission for first hit, if available
		path.ContributeEmissive();

		for (int depth = 0; depth < BounceLimit; depth++)
		{
			//Sample the bsdf
			var bounce = new Bounce(path.touch, distribution.Next2D());
			if (bounce.IsZero) break;

			Sample2D radiantSample = distribution.Next2D();
			foreach (ref var sample in lightSamples) sample = distribution.Next1D();

			//If the bounce is specular, then we do not use multiple importance sampling (MIS)
			if (bounce.IsSpecular)
			{
				//Check if the path is exhausted
				if (path.Advance(bounce, Survivability, distribution.Next1D())) break;

				//Begin finding a new bounce
				allocator.Restart();

				if (!path.FindNext(scene, allocator))
				{
					//None found, accumulate ambient and exit
					path.Contribute(EvaluateAllAmbient());
					break;
				}

				//Try add intersected emission
				path.ContributeEmissive();
			}
			else
			{
				//Select light from scene for MIS
				ILight light = scene.PickLight(lightSamples, allocator, out float lightPdf);

				float weight = 1f / lightPdf;
				var area = light as IAreaLight;

				path.Contribute(weight * ImportanceSampleRadiant(light, path.touch, radiantSample, scene, out bool mis));

				if (mis)
				{
					Assert.IsNotNull(area);
					weight *= PowerHeuristic(bounce.pdf, area!.ProbabilityDensity(path.touch.point, bounce.incident));
				}

				if (path.Advance(bounce, Survivability, distribution.Next1D())) break; //Path exhausted

				allocator.Restart();

				//Add ambient light and exit if no intersection
				if (!path.FindNext(scene, allocator))
				{
					if (area is AmbientLight ambient) path.Contribute(weight * ambient.Evaluate(path.CurrentDirection));
					break;
				}

				//TODO: hitting emissive surface immediately ends path

				//Try add emission with MIS
				//FIX: area light could get recollected after allocator restarts
				if (area is GeometryLight geometry && geometry.Token == path.touch.token)
				{
					path.ContributeEmissive(weight);
				}
			}
		}

		return path.Result;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		Float3 EvaluateAllAmbient() => scene.lights.EvaluateAmbient(path.CurrentDirection);
	}

	protected override ContinuousDistribution CreateDistribution(RenderProfile profile) => new StratifiedDistribution(profile.TotalSample);

	/// <summary>
	/// Importance samples <paramref name="light"/> at <paramref name="touch"/> and returns the sampled radiant.
	/// If multiple importance sampling is used, <paramref name="mis"/> will be assigned to true, otherwise false.
	/// </summary>
	static Float3 ImportanceSampleRadiant(ILight light, in Touch touch, Sample2D sample, PreparedScene scene, out bool mis)
	{
		//Importance sample light
		mis = light is IAreaLight;

		Float3 radiant = light.Sample(touch.point, sample, out Float3 incident, out float radiantPdf, out float travel);

		if (!FastMath.Positive(radiantPdf) | !radiant.PositiveRadiance()) return Float3.Zero;

		//Evaluate bsdf at the direction sampled for our light
		ref readonly Float3 outgoing = ref touch.outgoing;
		Float3 scatter = touch.bsdf.Evaluate(outgoing, incident);
		scatter *= touch.NormalDot(incident);

		//Conditionally terminate if radiant cannot be positive
		if (!scatter.PositiveRadiance()) return Float3.Zero;
		var query = touch.SpawnOcclude(incident, travel);
		if (scene.Occlude(ref query)) return Float3.Zero;

		//Calculate final radiant
		radiant *= 1f / radiantPdf;
		if (!mis) return scatter * radiant;

		float pdf = touch.bsdf.ProbabilityDensity(outgoing, incident);
		return PowerHeuristic(radiantPdf, pdf) * scatter * radiant;
	}

	/// <summary>
	/// Power heuristic with a constant power of two used for multiple importance sampling.
	/// NOTE: <paramref name="pdf0"/> will become the numerator, not <paramref name="pdf1"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static float PowerHeuristic(float pdf0, float pdf1) => pdf0 * pdf0 / (pdf0 * pdf0 + pdf1 * pdf1);

	struct Path
	{
		public Path(in Ray ray)
		{
			Result = Float3.Zero;
			energy = Float3.One;
			Unsafe.SkipInit(out touch);
			query = new TraceQuery(ray);
		}

		public Float3 Result { get; private set; }
		public Touch touch;

		TraceQuery query;
		Float3 energy;

		public Float3 CurrentDirection => query.ray.direction;

		public Material Material => touch.shade.material;

		public bool FindNext(PreparedScene scene, Allocator allocator)
		{
			while (scene.Trace(ref query))
			{
				touch = scene.Interact(query);
				allocator.Restart();

				Material.Scatter(ref touch, allocator);
				if (touch.bsdf != null) return true;

				query = query.SpawnTrace();
			}

			return false;
		}

		public bool Advance(in Bounce bounce, float survivability, Sample1D sample)
		{
			energy *= bounce.scatter / bounce.pdf;

			//Conditional path termination with Russian Roulette
			bool exhausted = RussianRoulette(ref energy, survivability, sample);
			if (!exhausted) query = query.SpawnTrace(bounce.incident);

			return exhausted;
		}

		public void Contribute(in Float3 value) => Result += energy * value;

		public void ContributeEmissive(float weight = 1f)
		{
			if (Material is not IEmissive emissive || !FastMath.Positive(emissive.Power)) return;
			Contribute(emissive.Emit(touch.point, touch.outgoing) * weight);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool RussianRoulette(ref Float3 energy, float survivability, Sample1D sample)
		{
			float luminance = PackedMath.GetLuminance(Utilities.ToVector(energy));
			float probability = FastMath.Clamp01(survivability * luminance);

			if (sample >= probability) return true;

			energy *= 1f / probability;
			return false;
		}
	}

	readonly struct Bounce
	{
		public Bounce(in Touch touch, Sample2D sample)
		{
			FunctionType type = TryExcludeSpecular(touch.bsdf);

			scatter = touch.bsdf.Sample
			(
				touch.outgoing, sample, out incident,
				out pdf, out function, type
			);

			scatter *= touch.NormalDot(incident);
		}

		public readonly Float3 scatter;
		public readonly Float3 incident;
		public readonly float pdf;
		readonly BxDF function;

		public bool IsZero => !FastMath.Positive(pdf) | !scatter.PositiveRadiance();

		public bool IsSpecular => function.type.Any(FunctionType.specular);

		/// <summary>
		/// Returns a <see cref="FunctionType"/> that tries to exclude all <see cref="BxDF"/> of type <see cref="FunctionType.specular"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static FunctionType TryExcludeSpecular(BSDF bsdf)
		{
			int count = bsdf.Count(FunctionType.specular);
			int total = bsdf.Count(FunctionType.all);

			return count == 0 || count == total ? FunctionType.all : ~FunctionType.specular;
		}
	}
}