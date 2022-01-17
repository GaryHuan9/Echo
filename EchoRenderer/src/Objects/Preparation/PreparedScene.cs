using System;
using System.Collections.Generic;
using System.Threading;
using CodeHelpers.Diagnostics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Objects.Lights;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.Profiles;

namespace EchoRenderer.Objects.Preparation
{
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
			foreach (Object child in source.LoopChildren(true))
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

			_lightSources = lightsList.ToArray();
			_ambientSources = ambientList.ToArray();

			preparer = new ScenePreparer(source, profile);

			//Create root instance and prepare materials
			rootInstance = new PreparedInstance(preparer, source);
			preparer.materials.Prepare();

			//Prepare lights
			foreach (LightSource light in LightSources) light.Prepare(this);

			DebugHelper.Log("Prepared scene");
		}

		public readonly ScenePreparer preparer;
		public readonly Scene source;
		public readonly Camera camera;

		readonly LightSource[] _lightSources;
		readonly AmbientLight[] _ambientSources;

		public ReadOnlySpan<LightSource> LightSources => _lightSources;
		public ReadOnlySpan<AmbientLight> AmbientSources => _ambientSources;

		long _traceCount;
		long _occludeCount;

		public long TraceCount => Interlocked.Read(ref _traceCount);
		public long OccludeCount => Interlocked.Read(ref _occludeCount);

		readonly PreparedInstance rootInstance;

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
		/// Begins interacting with the result of <paramref name="query"/> by creating and
		/// returning an <see cref="Interaction"/> and outputting <paramref name="material"/>.
		/// </summary>
		public Interaction Interact(in TraceQuery query, out Material material)
		{
			query.AssertHit();
			ref readonly var token = ref query.token;

			var instance = token.InstanceCount == 0 ? rootInstance : preparer.GetPreparedInstance(token.FinalInstanceId);
			return instance.pack.Interact(query, preparer, instance, out material);
		}

		/// <summary>
		/// Returns the approximated cost of computing a <see cref="TraceQuery"/> with <see cref="Trace"/>.
		/// </summary>
		public int TraceCost(in Ray ray)
		{
			float distance = float.PositiveInfinity;
			return rootInstance.TraceCost(ray, ref distance);
		}

		public void ResetIntersectionCount() => Interlocked.Exchange(ref _traceCount, 0);
	}
}