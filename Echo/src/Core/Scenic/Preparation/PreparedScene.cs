using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.Common.Mathematics;
using Echo.Common.Mathematics.Primitives;
using Echo.Common.Memory;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Evaluation.Distributions.Discrete;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Geometries;
using Echo.Core.Scenic.Lights;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Preparation;

/// <summary>
/// A <see cref="Scene"/> prepared ready for fast interactions.
/// </summary>
public class PreparedScene
{
	public PreparedScene(Scene scene, ScenePrepareProfile profile)
	{
		var lightsList = new List<LightSource>();

		//Gather important objects
		foreach (Entity child in scene.LoopChildren(true))
		{
			switch (child)
			{
				case Camera value:
				{
					if (camera == null) camera = value;
					else DebugHelper.Log($"Multiple {nameof(Camera)} found! Only the first one will be used.");

					break;
				}
				case LightSource value:
				{
					lightsList.Add(value);
					break;
				}
			}

			if (child.Scale.MinComponent <= 0f) throw new Exception($"Cannot have non-positive scales! '{child.Scale}'");
		}

		var preparer = new ScenePreparer(scene, profile);
		rootInstance = new PreparedInstanceRoot(preparer, scene);

		info = new Info(this, preparer);
		lights = new Lights(this, lightsList);
	}

	public readonly Camera camera;
	public readonly Lights lights;
	public readonly Info info;

	long _traceCount;
	long _occludeCount;

	public long TraceCount => Interlocked.Read(ref _traceCount);
	public long OccludeCount => Interlocked.Read(ref _occludeCount);

	readonly PreparedInstanceRoot rootInstance;

	public static readonly PreparedScene empty = new(new Scene(), new ScenePrepareProfile());

	/// <summary>
	/// Processes the <paramref name="query"/> and returns whether it intersected with something.
	/// </summary>
	public bool Trace(ref TraceQuery query)
	{
		if (!FastMath.Positive(query.distance)) return false;
		float original = query.distance;

		rootInstance.TraceRoot(ref query);
		Interlocked.Increment(ref _traceCount);
		return query.distance < original;
	}

	/// <summary>
	/// Processes the <paramref name="query"/> and returns whether it is occluded by something.
	/// </summary>
	public bool Occlude(ref OccludeQuery query)
	{
		if (!FastMath.Positive(query.travel)) return false;
		Interlocked.Increment(ref _occludeCount);
		return rootInstance.OccludeRoot(ref query);
	}

	/// <summary>
	/// Interacts with a concluded <see cref="TraceQuery"/> that was performed
	/// on this <see cref="PreparedScene"/> by creating a <see cref="Touch"/>.
	/// </summary>
	public Touch Interact(in TraceQuery query) => rootInstance.Interact(query);

	/// <summary>
	/// Picks an <see cref="ILight"/> in this <see cref="PreparedScene"/>.
	/// </summary>
	/// <param name="samples">The <see cref="ReadOnlySpan{T}"/> of <see cref="Sample1D"/> values used for sampling.</param>
	/// <param name="allocator">The <see cref="Allocator"/> used to allocate a <see cref="GeometryLight"/> if needed.</param>
	/// <returns>The <see cref="Probable{T}"/> light picked.</returns>
	public Probable<ILight> PickLight(ReadOnlySpan<Sample1D> samples, Allocator allocator)
	{
		Probable<LightSource> primary = lights.PickLightSource(samples[0]);
		if (primary.content != null) return (primary.content, primary.pdf);

		//Choose emissive geometry as light
		var light = allocator.New<GeometryLight>();

		Probable<GeometryToken> token = rootInstance.Find(samples[1..], out var instance);
		MaterialIndex index = instance.pack.GetMaterialIndex(token.content.Geometry);

		Material material = instance.GetMaterial(index);
		light.Reset(this, token, (IEmissive)material);
		return (light, primary.pdf * token.pdf);
	}

	/// <summary>
	/// Samples a <see cref="GeometryPoint"/> on an object in this <see cref="PreparedScene"/>.
	/// </summary>
	/// <param name="token">The <see cref="GeometryToken"/> that represents the object to be sampled.</param>
	/// <param name="origin">The world-space point of whose perspective the result should be sampled through.</param>
	/// <param name="sample">The <see cref="Sample2D"/> used the sample the result.</param>
	/// <returns>The <see cref="Probable{T}"/> world-space point that was sampled.</returns>
	public Probable<GeometryPoint> Sample(in GeometryToken token, in Float3 origin, Sample2D sample)
	{
		if (token.InstanceCount == 0) return rootInstance.pack.Sample(token.Geometry, origin, sample);

		//TODO: support the entire hierarchy
		throw new NotImplementedException();
	}

	/// <summary>
	/// Calculates the pdf of selecting <see cref="incident"/> on an object with <see cref="Sample"/>.
	/// </summary>
	/// <param name="token">The <see cref="GeometryToken"/> that represents the object.</param>
	/// <param name="origin">The world-space point of whose perspective the pdf should be calculated through.</param>
	/// <param name="incident">The selected world-space unit direction that points from <paramref name="origin"/> to the object.</param>
	/// <returns>The probability density function (pdf) value over solid angles of this selection.</returns>
	/// <seealso cref="Sample"/>
	public float ProbabilityDensity(in GeometryToken token, in Float3 origin, in Float3 incident)
	{
		if (token.InstanceCount == 0) return rootInstance.pack.ProbabilityDensity(token.Geometry, origin, incident);

		//TODO: support the entire hierarchy
		throw new NotImplementedException();
	}

	/// <summary>
	/// Returns the approximated cost of computing a <see cref="TraceQuery"/> with <see cref="Trace"/>.
	/// </summary>
	public int TraceCost(in Ray ray)
	{
		float distance = float.PositiveInfinity;
		return rootInstance.TraceCost(ray, ref distance);
	}

	public void ResetIntersectionCount()
	{
		Interlocked.Exchange(ref _traceCount, 0);
		Interlocked.Exchange(ref _occludeCount, 0);
	}

	public record Info
	{
		public Info(PreparedScene scene, ScenePreparer preparer)
		{
			scene.rootInstance.CalculateBounds(out aabb, out boundingSphere);

			depth = preparer.depth;
			instancedCounts = preparer.instancedCounts;
			uniqueCounts = preparer.uniqueCounts;
		}

		public readonly AxisAlignedBoundingBox aabb;
		public readonly BoundingSphere boundingSphere;

		public readonly int depth;
		public readonly GeometryCounts instancedCounts;
		public readonly GeometryCounts uniqueCounts;
	}

	public class Lights
	{
		public Lights(PreparedScene scene, IReadOnlyCollection<LightSource> all)
		{
			int length = all.Count;
			float geometryPower = scene.rootInstance.Power;

			if (length == 0 && !FastMath.Positive(geometryPower))
			{
				//Degenerate case with literally zero light contributor; our output image will literally be
				//completely black, but we add in a light so no exception is thrown when we look for lights.

				_ambient = Array.Empty<AmbientLight>();
				_all = new LightSource[] { new PointLight { Intensity = RGB128.Black } };
				distribution = new DiscreteDistribution1D(stackalloc[] { 1f, 0f });

				return;
			}

			_all = all.ToArray();

			var ambientList = new List<AmbientLight>();

			//Prepare power values for distribution
			using var _ = Pool<float>.Fetch(length + 1, out var powerValues);

			for (int i = 0; i < length; i++)
			{
				var light = _all[i];
				light.Prepare(scene);

				float power = light.Power;
				Assert.IsTrue(power > 0f);
				powerValues[i] = power;

				if (light is AmbientLight ambient) ambientList.Add(ambient);
			}

			_ambient = ambientList.ToArray();
			powerValues[length] = geometryPower;

			distribution = new DiscreteDistribution1D(powerValues);
		}

		readonly DiscreteDistribution1D distribution;

		readonly LightSource[] _all;
		readonly AmbientLight[] _ambient;

		/// <summary>
		/// Accesses all of the <see cref="LightSource"/> present in a <see cref="PreparedScene"/>, which includes all <see cref="Ambient"/>.
		/// </summary>
		public ReadOnlySpan<LightSource> All => _all;

		/// <summary>
		/// Accesses all of the <see cref="AmbientLight"/> present in a <see cref="PreparedScene"/>.
		/// </summary>
		public ReadOnlySpan<AmbientLight> Ambient => _ambient;

		/// <summary>
		/// Picks a <see cref="LightSource"/> based on <paramref name="sample"/>.
		/// </summary>
		/// <param name="sample">The <see cref="Sample1D"/> used to select our <see cref="LightSource"/>.</param>
		/// <returns>The <see cref="Probable{T}"/> light picked, or null if <see cref="GeometryLight"/> was picked.</returns>
		/// <remarks>Even if a <see cref="GeometryLight"/> was picked and <see cref="Probable{T}.content"/> is null,
		/// <see cref="Probable{T}.pdf"/> will still contain the probability density function (pdf) value as per usual.</remarks>
		public Probable<LightSource> PickLightSource(Sample1D sample)
		{
			Probable<int> index = distribution.Pick(sample);
			return (index < _all.Length ? _all[index] : null, index.pdf);
		}

		/// <summary>
		/// Evaluates all of the <see cref="AmbientLight"/> at <paramref name="direction"/> in world-space.
		/// </summary>
		public RGB128 EvaluateAmbient(Float3 direction)
		{
			var total = RGB128.Black;

			foreach (AmbientLight light in _ambient) total += light.Evaluate(direction);

			return total;
		}
	}
}