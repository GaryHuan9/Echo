using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Scenic.Geometric;
using Echo.Core.Scenic.Instancing;
using Echo.Core.Scenic.Lighting;

namespace Echo.Core.Scenic.Preparation;

partial record ScenePreparerNew
{
	class Node
	{
		public Node(EntityPack source)
		{
			this.source = source;

			ancestors = new HashSet<Node> { this };

			foreach (Entity entity in source.LoopChildren(true))
			{
				if (entity is IGeometrySource geometry) geometrySources.Add(geometry);
				if (entity is ILightSource light) lightSources.Add(light);

				if (entity is PackInstance { EntityPack: { } } instance)
				{
					if (!children.TryGetValue(instance.EntityPack, out var pair))
					{
						pair = (null, new List<PackInstance>());
						children.Add(instance.EntityPack, pair);
					}

					pair.Instances.Add(instance);
				}
			}
		}

		readonly EntityPack source;

		readonly List<IGeometrySource> geometrySources = new();
		readonly List<ILightSource> lightSources = new();

		readonly HashSet<Node> ancestors;
		readonly Dictionary<EntityPack, (Node Node, List<PackInstance> Instances)> children = new();

		SwatchExtractor swatchExtractor;
		PreparedPack preparedPack;

		public IEnumerable<EntityPack> InstancingPacks => children.Keys;

		public void AddChild(Node node)
		{
			if (!children.TryGetValue(node.source, out var pair)) throw new ArgumentException("Unrecognized source node.", nameof(node));
			if (ancestors.Contains(node)) throw new ArgumentException($"Found recursively instanced {nameof(EntityPack)}.", nameof(node));
			if (pair.Node == node) return;

			children[node.source] = pair with { Node = this };
			node.ancestors.UnionWith(ancestors);
		}

		public void CreatePreparedPack(ScenePreparerNew preparer)
		{
			swatchExtractor = new SwatchExtractor(preparer);

			preparedPack = new PreparedPack
			(
				CollectionsMarshal.AsSpan(geometrySources), CollectionsMarshal.AsSpan(lightSources),
				CreatePreparedInstances(preparer), preparer.AcceleratorCreator, swatchExtractor
			);
		}

		public PreparedSceneNew CreatePreparedScene(ScenePreparerNew preparer)
		{
			var localSwatchExtractor = new SwatchExtractor(preparer);

			return new PreparedSceneNew
			(
				CollectionsMarshal.AsSpan(geometrySources), CollectionsMarshal.AsSpan(lightSources),
				CreatePreparedInstances(preparer), preparer.AcceleratorCreator, localSwatchExtractor
			);
		}

		ImmutableArray<PreparedInstance> CreatePreparedInstances(ScenePreparerNew preparer)
		{
			var builder = ImmutableArray.CreateBuilder<PreparedInstance>();

			foreach ((_, (Node node, List<PackInstance> instances)) in children)
			{
				if (node == null) throw new InvalidOperationException($"Missing child for {nameof(EntityPack)}.");
				foreach (PackInstance instance in instances) builder.Add(CreatePreparedInstance(preparer, instance));
			}

			return builder.ToImmutable();
		}

		static PreparedInstance CreatePreparedInstance(ScenePreparerNew preparer, PackInstance source)
		{
			Node node = preparer.CreateOrGetNode(source.EntityPack);

			var pack = node.preparedPack;
			var extractor = node.swatchExtractor;

			if (pack != null && extractor != null) return new PreparedInstance(pack, extractor.Prepare(source.Swatch), source.InverseTransform);
			throw new InvalidOperationException($"Cannot create {nameof(PreparedInstance)} because the source {nameof(PreparedPack)} is missing.");
		}
	}
}