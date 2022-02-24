using System;
using System.Collections.Generic;
using System.Threading;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Preparation;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Distributions;
using EchoRenderer.Core.Scenic.Lights;

namespace EchoRenderer.Core.Scenic.Preparation;

/// <summary>
/// A <see cref="Scene"/> prepared ready for fast interactions.
/// </summary>
public class PreparedScene
{
	public PreparedScene(Scene source, ScenePrepareProfile profile)
	{
		this.source = source;

		var lightsList = new List<LightSource>();
		var ambientList = new List<AmbientLight>();

		//Gather important objects
		foreach (Entity child in source.LoopChildren(true))
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
					if (value is AmbientLight ambient) ambientList.Add(ambient);

					break;
				}
			}

			if (child.Scale.MinComponent <= 0f) throw new Exception($"Cannot have non-positive scales! '{child.Scale}'");
		}

		preparer = new ScenePreparer(source, profile);
		preparer.PrepareAll();

		//Create root instance
		rootInstance = new PreparedInstanceRoot(preparer, source);
		rootInstance.CalculateBounds(out aabb, out boundingSphere);

		//Prepare lights
		lightSources = lightsList.ToArray();
		_ambientLights = ambientList.ToArray();

		foreach (LightSource light in lightSources)
		{
			light.Prepare(this);

			float power = light.Power;
		}

		DebugHelper.Log("Prepared scene");
	}

	public readonly ScenePreparer preparer; //NOTE: this field should be removed in the future, it is only here now for temporary access to scene preparation data
	public readonly Scene source;
	public readonly Camera camera;

	public readonly AxisAlignedBoundingBox aabb;
	public readonly BoundingSphere boundingSphere;

	readonly LightSource[] lightSources;
	readonly AmbientLight[] _ambientLights;

	public ReadOnlySpan<AmbientLight> AmbientLights => _ambientLights;

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
	/// Returns the approximated cost of computing a <see cref="TraceQuery"/> with <see cref="Trace"/>.
	/// </summary>
	public int TraceCost(in Ray ray)
	{
		float distance = float.PositiveInfinity;
		return rootInstance.TraceCost(ray, ref distance);
	}

	/// <summary>
	/// Interacts with the result of <paramref name="query"/> by returning an <see cref="Interaction"/>.
	/// </summary>
	public Interaction Interact(in TraceQuery query)
	{
		query.AssertHit();

		PreparedInstance instance = rootInstance;
		Float4x4 transform = Float4x4.identity;

		//Traverse down the instancing path
		foreach (ref readonly NodeToken token in query.token.Instances)
		{
			//Because we traverse in reverse, we must also multiply the transform in reverse
			transform = instance.inverseTransform * transform;
			instance = instance.pack.GetInstance(token);
		}

		return instance.pack.Interact(query, transform, instance);
	}

	public ILight PickLight(Arena arena, out float pdf)
	{
		//Handle degenerate cases
		int length = lightSources.Length;

		if (length == 0)
		{
			pdf = 0f;
			return null;
		}

		//Finds one light to sample
		pdf = 1f / length;

		// throw new NotImplementedException();
		return lightSources[arena.distribution.NextOne().Range(length)];
	}

	/// <summary>
	/// Samples the object represented by <paramref name="token"/> from the perspective of <paramref name="origin"/> and
	/// outputs the probability density function <paramref name="pdf"/> over solid angles from <paramref name="origin"/>.
	/// </summary>
	public GeometryPoint Sample(in GeometryToken token, in Float3 origin, Distro2 distro, out float pdf)
	{
		if (token.InstanceCount == 0) return rootInstance.pack.Sample(token.Geometry, origin, distro, out pdf);

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

	public void ResetIntersectionCount()
	{
		Interlocked.Exchange(ref _traceCount, 0);
		Interlocked.Exchange(ref _occludeCount, 0);
	}
}