using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Preparation;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Distributions;
using EchoRenderer.Core.Rendering.Distributions.Discrete;
using EchoRenderer.Core.Rendering.Materials;
using EchoRenderer.Core.Scenic.Instancing;
using EchoRenderer.Core.Scenic.Lights;

namespace EchoRenderer.Core.Scenic.Preparation;

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

		preparer = new ScenePreparer(scene, profile);
		preparer.Prepare();

		//Create root instance
		rootInstance = new PreparedInstanceRoot(preparer, scene);
		rootInstance.CalculateBounds(out aabb, out boundingSphere);

		//Prepare lights
		lights = new Lights(this, lightsList);
		DebugHelper.Log("Prepared scene");
	}

	public readonly ScenePreparer preparer; //NOTE: this field should be removed in the future, it is only here now for temporary access to scene preparation data
	public readonly Camera camera;
	public readonly Lights lights;

	public readonly AxisAlignedBoundingBox aabb;
	public readonly BoundingSphere boundingSphere;

	long _traceCount;
	long _occludeCount;

	public long TraceCount => Interlocked.Read(ref _traceCount);
	public long OccludeCount => Interlocked.Read(ref _occludeCount);

	readonly PreparedInstanceRoot rootInstance;

	/// <summary>
	/// Processes the <paramref name="query"/> and returns whether it intersected with something.
	/// </summary>
	public bool Trace(ref TraceQuery query)
	{
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
		Interlocked.Increment(ref _occludeCount);
		return rootInstance.OccludeRoot(ref query);
	}

	/// <summary>
	/// Interacts with a concluded <see cref="TraceQuery"/> that was performed
	/// on this <see cref="PreparedScene"/> by creating a <see cref="Touch"/>.
	/// </summary>
	public Touch Interact(in TraceQuery query) => rootInstance.Interact(query);

	/// <summary>
	/// Picks a <see cref="ILight"/> in this <see cref="PreparedScene"/> and outputs its probability density function to <paramref name="pdf"/>.
	/// </summary>
	[SkipLocalsInit]
	public ILight PickLight(Arena arena, out float pdf)
	{
		Sample1D sample = arena.Distribution.Next1D();
		var source = lights.PickLightSource(sample, out pdf);

		if (source != null) return source;

		//Choose emissive geometry as light
		Span<Sample1D> samples = stackalloc Sample1D[EntityPack.MaxLayer];
		foreach (ref var one in samples) one = arena.Distribution.Next1D();

		var light = arena.allocator.New<GeometryLight>();

		GeometryToken token = rootInstance.Find(samples, out var instance, out float tokenPDF);
		Material material = instance.GetMaterial(instance.pack.GetMaterialIndex(token.Geometry));

		light.Reset(this, token, material);

		pdf *= tokenPDF;
		return light;
	}

	/// <summary>
	/// Samples the object represented by <paramref name="token"/> from the perspective of <paramref name="origin"/> and
	/// outputs the probability density function <paramref name="pdf"/> over solid angles from <paramref name="origin"/>.
	/// </summary>
	public GeometryPoint Sample(in GeometryToken token, in Float3 origin, Sample2D sample, out float pdf)
	{
		if (token.InstanceCount == 0) return rootInstance.pack.Sample(token.Geometry, origin, sample, out pdf);

		//TODO: support the entire hierarchy
		throw new NotImplementedException();
	}

	/// <summary>
	/// On the object represented by <paramref name="token"/>, returns the probability density function
	/// (pdf) over solid angles of sampling <paramref name="incident"/> from <paramref name="origin"/>.
	/// </summary>
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

	public class Lights
	{
		public Lights(PreparedScene scene, List<LightSource> all)
		{
			_all = all.ToArray();
			int length = _all.Length;

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

			powerValues[length] = scene.rootInstance.Power;
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
		/// Selects a <see cref="LightSource"/> that is in <see cref="PreparedScene"/> based on <paramref name="sample"/>.
		/// If a <see cref="GeometryLight"/> is selected, null is returned. <paramref name="pdf"/> contains the probability
		/// density function value of this selection.
		/// </summary>
		public LightSource PickLightSource(Sample1D sample, out float pdf)
		{
			// int index = sample.Range(_all.Length); pdf = 1f / _all.Length;

			int index = distribution.Find(sample, out pdf);
			return index < _all.Length ? _all[index] : null;
		}

		/// <summary>
		/// Evaluates all of the <see cref="AmbientLight"/> at <paramref name="direction"/> in world-space.
		/// </summary>
		public Float3 EvaluateAmbient(Float3 direction)
		{
			Float3 total = Float3.zero;

			foreach (AmbientLight light in _ambient) total += light.Evaluate(direction);

			return total;
		}
	}
}