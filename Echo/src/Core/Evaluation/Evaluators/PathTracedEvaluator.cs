using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.Common.Mathematics;
using Echo.Common.Mathematics.Primitives;
using Echo.Common.Memory;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Evaluation.Distributions.Continuous;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Scenic.Lights;
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
	/// The survivability of a path with during unbiased path termination. As this value goes up, the amount of variance decreases,
	/// and the time spend on each path increases. This value should be relatively high for interior scenes while low for outdoors.
	/// </summary>
	public float Survivability { get; init; } = 2.5f;

	//NOTE: although the word 'radiant' is frequently used here to denote particles of energy accumulating,
	//technically it is not correct because the size of the emitter could be either a point or an area,
	//(so for the same reason the word 'radiance' is also wrong) we just chose 'radiant' because it has the
	//same length as the word 'scatter'.

	public override IEvaluationLayer CreateOrClearLayer(RenderBuffer buffer) => CreateOrClearLayer<RGB128>(buffer, "path");

	public override Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator)
	{
		var path = new Path(ray);

		//Quick exit with ambient light if no intersection
		if (!path.Advance(scene, allocator)) return EvaluateAllAmbient();

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
				if (!path.Continue(bounce, Survivability, distribution.Next1D())) break;

				//Begin finding a new bounce
				allocator.Restart();

				if (!path.Advance(scene, allocator))
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
				(ILight light, float lightPdf) = scene.PickLight(lightSamples, allocator);

				float weight = 1f / lightPdf;
				var area = light as IAreaLight;

				path.Contribute(weight * ImportanceSampleRadiant(light, path.touch, radiantSample, scene, out bool mis));

				if (mis)
				{
					Assert.IsNotNull(area);
					weight *= PowerHeuristic(bounce.pdf, area!.ProbabilityDensity(path.touch.point, bounce.incident));
				}

				if (!path.Continue(bounce, Survivability, distribution.Next1D())) break; //Path exhausted

				allocator.Restart();

				//Add ambient light and exit if no intersection
				if (!path.Advance(scene, allocator))
				{
					if (area is AmbientLight ambient) path.Contribute(weight * ambient.Evaluate(path.CurrentDirection));
					break;
				}

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
		RGB128 EvaluateAllAmbient() => scene.lights.EvaluateAmbient(path.CurrentDirection);
	}

	/// <summary>
	/// Importance samples <paramref name="light"/> at <paramref name="touch"/> and returns the sampled radiant.
	/// If multiple importance sampling is used, <paramref name="mis"/> will be assigned to true, otherwise false.
	/// </summary>
	static RGB128 ImportanceSampleRadiant(ILight light, in Touch touch, Sample2D sample, PreparedScene scene, out bool mis)
	{
		//Importance sample light
		mis = light is IAreaLight;

		(RGB128 radiant, float radiantPdf) = light.Sample(touch.point, sample, out Float3 incident, out float travel);

		if (!FastMath.Positive(radiantPdf) | radiant.IsZero) return RGB128.Black;

		//Evaluate bsdf at the direction sampled for our light
		ref readonly Float3 outgoing = ref touch.outgoing;
		RGB128 scatter = touch.bsdf.Evaluate(outgoing, incident);
		scatter *= touch.NormalDot(incident);

		//Conditionally terminate if radiant cannot be positive
		if (scatter.IsZero) return RGB128.Black;
		var query = touch.SpawnOcclude(incident, travel);
		if (scene.Occlude(ref query)) return RGB128.Black;

		//Calculate final radiant
		radiant /= radiantPdf;
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
			Result = RGB128.Black;
			energy = RGB128.White;
			Unsafe.SkipInit(out touch);
			query = new TraceQuery(ray);
		}

		public RGB128 Result { get; private set; }
		public Touch touch;

		RGB128 energy;
		TraceQuery query;

		public readonly Float3 CurrentDirection => query.ray.direction;

		public readonly Material Material => touch.shade.material;

		public bool Advance(PreparedScene scene, Allocator allocator)
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

		public bool Continue(in Bounce bounce, float survivability, Sample1D sample)
		{
			energy *= bounce.scatter / bounce.pdf;

			//Conditional path termination with Russian Roulette
			bool survived = RussianRoulette(ref energy, survivability, sample);
			if (survived) query = query.SpawnTrace(bounce.incident);

			return survived;
		}

		public void Contribute(in RGB128 value) => Result += energy * value; //OPTIMIZE: fma

		public void ContributeEmissive(float weight = 1f)
		{
			if (Material is not IEmissive emissive || !FastMath.Positive(emissive.Power)) return;
			Contribute(emissive.Emit(touch.point, touch.outgoing) * weight);
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

	readonly struct Bounce
	{
		public Bounce(in Touch touch, Sample2D sample)
		{
			(scatter, pdf) = touch.bsdf.Sample
			(
				touch.outgoing, sample, out incident,
				out function, TryExcludeSpecular(touch.bsdf)
			);

			scatter *= touch.NormalDot(incident);
		}

		public readonly RGB128 scatter;
		public readonly Float3 incident;
		public readonly float pdf;
		readonly BxDF function;

		public bool IsZero => !FastMath.Positive(pdf) | scatter.IsZero;

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