using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
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
		if (!path.Advance(scene, allocator)) return EvaluateAllAmbient();

		//Add emission for first hit, if available
		path.ContributeEmissive();

		for (int depth = 0; depth < BounceLimit; depth++)
		{
			//Sample the bsdf
			var bounce = new Bounce(path.touch, distribution.Next2D());
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
					path.Contribute(EvaluateAllAmbient());
					break;
				}

				//Try add intersected emission
				path.ContributeEmissive();
			}
			else
			{
				//Select light from scene for MIS
				(ILight light, float lightPdf) = scene.PickLight(lightSample, allocator);

				float weight = 1f / lightPdf;
				var area = light as IAreaLight;
				bool mis = area != null;

				//Importance sample the selected light
				RGB128 radiant = ImportanceSampleRadiant(light, path.touch, radiantSample, scene, mis);

				if (mis)
				{
					//Perform MIS between scatter and radiant
					path.Contribute(weight * radiant);

					float radiantPdf = area.ProbabilityDensity(path.touch.point, bounce.incident);
					weight *= PowerHeuristic(bounce.scatterPdf, radiantPdf);

					//Begin continue path to the next vertex and exit if path exhausted
					if (!path.Continue(bounce, Survivability, survivalSample)) break;

					//Cache GeometryToken from GeometryLight to potentially add emission after advancing
					TokenHierarchy token;
					bool hasToken;

					if (area is not GeometryLight geometry)
					{
						Unsafe.SkipInit(out token);
						hasToken = false;
					}
					else
					{
						token = geometry.Token;
						hasToken = true;
					}

					//Add ambient light and exit if no intersection
					if (!path.Advance(scene, allocator))
					{
						if (area is AmbientLight ambient) path.Contribute(weight * ambient.Evaluate(path.CurrentDirection));

						break;
					}

					//Try add emission with MIS
					if (hasToken && token == path.touch.token) path.ContributeEmissive(weight);
				}
				else
				{
					//Our light does not like MIS either, so no MIS is performed
					path.Contribute(weight * radiant);

					//Begin continue path to the next vertex and exit if path exhausted
					if (!path.Continue(bounce, Survivability, survivalSample)) break;

					//Add ambient light and exit if no intersection
					if (!path.Advance(scene, allocator)) break;
				}
			}
		}

		return path.Result;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		RGB128 EvaluateAllAmbient() => scene.lights.EvaluateAmbient(path.CurrentDirection);
	}

	/// <summary>
	/// Importance samples an <see cref="ILight"/>.
	/// </summary>
	/// <param name="light">The <see cref="ILight"/> to sample.</param>
	/// <param name="touch">The <see cref="Touch"/> to sample from.</param>
	/// <param name="sample">The <see cref="Sample2D"/> value to use.</param>
	/// <param name="scene">The <see cref="PreparedScene"/> that all of this takes place.</param>
	/// <param name="mis">Whether this method should use multiple importance sampling.</param>
	/// <returns>The sampled radiant from the <see cref="ILight"/>.</returns>
	static RGB128 ImportanceSampleRadiant(ILight light, in Touch touch, Sample2D sample, PreparedScene scene, bool mis)
	{
		//Importance sample light
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

		float scatterPdf = touch.bsdf.ProbabilityDensity(outgoing, incident);
		return PowerHeuristic(radiantPdf, scatterPdf) * scatter * radiant;
	}

	/// <summary>
	/// Power heuristic with a constant power of two used for multiple importance sampling.
	/// NOTE: <paramref name="pdf0"/> will become the numerator, not <paramref name="pdf1"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static float PowerHeuristic(float pdf0, float pdf1) => pdf0 * pdf0 / (pdf0 * pdf0 + pdf1 * pdf1);

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
			Unsafe.SkipInit(out touch);
			query = new TraceQuery(ray);
		}

		/// <summary>
		/// The accumulating result of this <see cref="Path"/>.
		/// </summary>
		public RGB128 Result { get; private set; }

		RGB128 energy;

		/// <summary>
		/// The current <see cref="Touch"/> experienced on this <see cref="Path"/>.
		/// </summary>
		public Touch touch;

		TraceQuery query;

		/// <summary>
		/// The current <see cref="Float3"/> direction of this <see cref="Path"/>.
		/// </summary>
		public readonly Float3 CurrentDirection => query.ray.direction;

		readonly Material Material => touch.shade.material;

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
				touch = scene.Interact(query);

				Material.Scatter(ref touch, allocator);
				if (touch.bsdf != null) return true;

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
		/// Contributes the emission of the <see cref="Material"/> of the current <see cref="touch"/>.
		/// </summary>
		/// <param name="weight">An optionally provided value to scale this contribution.</param>
		public void ContributeEmissive(float weight = 1f)
		{
			if (Material is not IEmissive emissive) return;
			if (!FastMath.Positive(emissive.Power)) return;

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

	/// <summary>
	/// A scattering event occured when a <see cref="Path"/> is traced.
	/// </summary>
	readonly struct Bounce
	{
		public Bounce(in Touch touch, Sample2D sample)
		{
			(scatter, scatterPdf) = touch.bsdf.Sample
			(
				touch.outgoing, sample, out incident,
				out function, TryExcludeSpecular(touch.bsdf)
			);

			scatter *= touch.NormalDot(incident);
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