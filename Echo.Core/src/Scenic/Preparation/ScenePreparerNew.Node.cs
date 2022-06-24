using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Scenic.Geometric;
using Echo.Core.Scenic.Instancing;
using Echo.Core.Scenic.Lighting;

namespace Echo.Core.Scenic.Preparation;

partial class ScenePreparerNew
{
	class Node
	{
		public Node(EntityPack source)
		{
			instanceSources = new List<PackInstance>();
			geometrySources = new List<IGeometrySource>();
			lightSources = new List<ILightSource>();

			foreach (Entity entity in source.LoopChildren(true))
			{
				if (entity is PackInstance { EntityPack: { } } instance)
				{
					instanceSources.Add(instance);
				}

				if (entity is IGeometrySource geometry) geometrySources.Add(geometry);
				if (entity is ILightSource light) lightSources.Add(light);
			}

			foreach (PackInstance instance in InstanceSources)
			{
				instance.EntityPack
			}

			ImmutableList.Create()
		}

		public readonly EntityPack source;
		public int InstancingCount { get; private set; }

		readonly List<PackInstance> instanceSources;
		readonly List<IGeometrySource> geometrySources;
		readonly List<ILightSource> lightSources;

		public readonly ImmutableHashSet<EntityPack> instancingPacks;

		public ReadOnlySpan<PackInstance> InstanceSources => CollectionsMarshal.AsSpan(instanceSources);
		public ReadOnlySpan<IGeometrySource> GeometrySources => CollectionsMarshal.AsSpan(geometrySources);
		public ReadOnlySpan<ILightSource> LightSources => CollectionsMarshal.AsSpan(lightSources);
	}
}