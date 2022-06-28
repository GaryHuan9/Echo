using System;
using System.Collections.Generic;
using System.Linq;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Evaluation.Distributions.Discrete;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Geometric;
using Echo.Core.Scenic.Lighting;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Preparation;

/// <summary>
/// A <see cref="Scene"/> prepared ready for fast interactions.
/// </summary>
public class PreparedSceneOld
{
	public PreparedSceneOld(Scene scene, ScenePrepareProfile profile)
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

		var preparer = new ScenePreparerOld(scene, profile);
		rootInstance = new PreparedInstanceRoot(preparer, scene);

		info = new Info(this, preparer);
		lights = new Lights(this, lightsList);
	}

	public readonly Camera camera;
	public readonly Lights lights;
	public readonly Info info;

	readonly PreparedInstanceRoot rootInstance;

	/// <summary>
	/// Processes the <paramref name="query"/> and returns whether it intersected with something.
	/// </summary>
	public bool Trace(ref TraceQuery query)
	{
		if (!FastMath.Positive(query.distance)) return false;
		float original = query.distance;

		rootInstance.TraceRoot(ref query);
		return query.distance < original;
	}

	/// <summary>
	/// Processes the <paramref name="query"/> and returns whether it is occluded by something.
	/// </summary>
	public bool Occlude(ref OccludeQuery query)
	{
		if (!FastMath.Positive(query.travel)) return false;
		return rootInstance.OccludeRoot(ref query);
	}

	/// <summary>
	/// Interacts with a concluded <see cref="TraceQuery"/> that was performed
	/// on this <see cref="PreparedSceneOld"/> by creating a <see cref="Touch"/>.
	/// </summary>
	public Touch Interact(in TraceQuery query) => rootInstance.Interact(query);

	/// <summary>
	/// Picks an <see cref="ILight"/> in this <see cref="PreparedSceneOld"/>.
	/// </summary>
	/// <param name="sample">The <see cref="Sample1D"/> value used to pick the result.</param>
	/// <param name="allocator">The <see cref="Allocator"/> used to allocate a <see cref="GeometryLight"/> if needed.</param>
	/// <returns>The <see cref="Probable{T}"/> light picked.</returns>
	public Probable<ILight> PickLight(Sample1D sample, Allocator allocator)
	{
		Probable<LightSource> primary = lights.PickLightSource(ref sample);
		if (primary.content != null) return (primary.content, primary.pdf);

		//Choose emissive geometry as light
		Probable<TokenHierarchy> token = rootInstance.Pick(sample, out var instance);
		MaterialIndex index = instance.pack.GetMaterialIndex(token.content.TopToken);

		var light = allocator.New<GeometryLight>();

		Material material = instance.GetMaterial(index);
		light.Reset(this, token, (IEmissive)material);
		return (light, primary.pdf * token.pdf);
	}

	/// <summary>
	/// Samples a <see cref="GeometryPoint"/> on an object in this <see cref="PreparedSceneOld"/>.
	/// </summary>
	/// <param name="token">The <see cref="TokenHierarchy"/> that represents the object to be sampled.</param>
	/// <param name="origin">The world-space point of whose perspective the result should be sampled through.</param>
	/// <param name="sample">The <see cref="Sample2D"/> used the sample the result.</param>
	/// <returns>The <see cref="Probable{T}"/> world-space point that was sampled.</returns>
	public Probable<GeometryPoint> Sample(in TokenHierarchy token, in Float3 origin, Sample2D sample)
	{
		if (token.InstanceCount == 0) return rootInstance.pack.Sample(token.TopToken, origin, sample);

		//TODO: support the entire hierarchy
		throw new NotImplementedException();
	}

	/// <summary>
	/// Calculates the pdf of selecting <see cref="incident"/> on an object with <see cref="Sample"/>.
	/// </summary>
	/// <param name="token">The <see cref="TokenHierarchy"/> that represents the object.</param>
	/// <param name="origin">The world-space point of whose perspective the pdf should be calculated through.</param>
	/// <param name="incident">The selected world-space unit direction that points from <paramref name="origin"/> to the object.</param>
	/// <returns>The probability density function (pdf) value over solid angles of this selection.</returns>
	/// <seealso cref="Sample"/>
	public float ProbabilityDensity(in TokenHierarchy token, in Float3 origin, in Float3 incident)
	{
		if (token.InstanceCount == 0) return rootInstance.pack.ProbabilityDensity(token.TopToken, origin, incident);

		//TODO: support the entire hierarchy
		throw new NotImplementedException();
	}

	/// <summary>
	/// Returns the approximated cost of computing a <see cref="TraceQuery"/> with <see cref="Trace"/>.
	/// </summary>
	public uint TraceCost(in Ray ray)
	{
		float distance = float.PositiveInfinity;
		return rootInstance.TraceCost(ray, ref distance);
	}

	public record Info
	{
		public Info(PreparedSceneOld scene, ScenePreparerOld preparer)
		{
			scene.rootInstance.CalculateBounds(out aabb, out boundingSphere);

			depth = preparer.depth;
			instancedCounts = preparer.instancedCounts;
			uniqueCounts = preparer.uniqueCounts;
			materialCount = preparer.MaterialCount;
			entityPackCount = preparer.EntityPackCount;
		}

		public readonly AxisAlignedBoundingBox aabb;
		public readonly BoundingSphere boundingSphere;

		public readonly int depth;
		public readonly GeometryCounts instancedCounts;
		public readonly GeometryCounts uniqueCounts;
		public readonly int materialCount;
		public readonly int entityPackCount;
	}

	public class Lights
	{
		public Lights(PreparedSceneOld scene, IReadOnlyCollection<LightSource> all)
		{
			int length = all.Count;
			GeometryPower = scene.rootInstance.Power;

			if (length == 0 && !FastMath.Positive(GeometryPower))
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
			powerValues[length] = GeometryPower;

			distribution = new DiscreteDistribution1D(powerValues);
		}

		readonly DiscreteDistribution1D distribution;

		readonly LightSource[] _all;
		readonly AmbientLight[] _ambient;

		/// <summary>
		/// Accesses all of the <see cref="LightSource"/> present in a <see cref="PreparedSceneOld"/>, which includes all <see cref="Ambient"/>.
		/// </summary>
		public ReadOnlySpan<LightSource> All => _all;

		/// <summary>
		/// Accesses all of the <see cref="AmbientLight"/> present in a <see cref="PreparedSceneOld"/>.
		/// </summary>
		public ReadOnlySpan<AmbientLight> Ambient => _ambient;

		/// <summary>
		/// The approximated total power of all the lights. 
		/// </summary>
		public float TotalPower => distribution.sum;

		/// <summary>
		/// The approximated total power of all emissive geometries.
		/// </summary>
		public float GeometryPower { get; }

		/// <summary>
		/// Picks a <see cref="LightSource"/> based on <paramref name="sample"/>.
		/// </summary>
		/// <param name="sample">The <see cref="Sample1D"/> used to select our <see cref="LightSource"/>.</param>
		/// <returns>The <see cref="Probable{T}"/> light picked, or null if <see cref="GeometryLight"/> was picked.</returns>
		/// <remarks>If a <see cref="GeometryLight"/> was picked, meaning that <see cref="Probable{T}.content"/> is null,
		/// <see cref="Probable{T}.pdf"/> will still contain the probability density function (pdf) value as per usual,
		/// and <paramref name="sample"/> will be readjusted to be uniform again through <see cref="Sample1D.Stretch"/>.</remarks>
		public Probable<LightSource> PickLightSource(ref Sample1D sample)
		{
			(int index, float pdf) = distribution.Pick(sample);
			if (index < _all.Length) return (_all[index], pdf);
			sample = sample.Stretch(1f - pdf, 1f);
			return (null, pdf);
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