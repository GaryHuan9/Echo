using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Scenic.Cameras;
using Echo.Core.Scenic.Geometries;
using Echo.Core.Scenic.Hierarchies;
using Echo.Core.Scenic.Lights;

namespace Echo.Core.Scenic.Preparation;

partial record ScenePreparer
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

				if (entity is PackInstance { Pack: { } } instance)
				{
					if (!children.TryGetValue(instance.Pack, out var pair))
					{
						pair = (null, new List<PackInstance>());
						children.Add(instance.Pack, pair);
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

		public void CreatePreparedPack(ScenePreparer preparer)
		{
			swatchExtractor = new SwatchExtractor(preparer);

			preparedPack = new PreparedPack
			(
				CollectionsMarshal.AsSpan(geometrySources), CollectionsMarshal.AsSpan(lightSources),
				CreatePreparedInstances(preparer), preparer.AcceleratorCreator, swatchExtractor
			);
		}

		public PreparedScene CreatePreparedScene(ScenePreparer preparer, string cameraName)
		{
			var lights = new List<InfiniteLight>();
			var cameras = new List<Camera>();

			foreach (Entity entity in source.LoopChildren(true))
			{
				if (entity is InfiniteLight light) lights.Add(light);
				else if (entity is Camera camera) cameras.Add(camera);
			}

			int cameraIndex = cameras.Count == 0 ? -1 : 0;

			if (cameraName != null) cameraIndex = cameras.FindIndex(camera => camera.Name == cameraName);
			if (cameraIndex < 0) throw new InvalidOperationException($"Cannot find a matching {nameof(Camera)}.");

			return new PreparedScene
			(
				CollectionsMarshal.AsSpan(geometrySources), CollectionsMarshal.AsSpan(lightSources),
				CreatePreparedInstances(preparer), preparer.AcceleratorCreator,
				new SwatchExtractor(preparer), lights, cameras[cameraIndex]
			);
		}

		ImmutableArray<PreparedInstance> CreatePreparedInstances(ScenePreparer preparer)
		{
			var builder = ImmutableArray.CreateBuilder<PreparedInstance>();

			foreach ((Node node, List<PackInstance> instances) in children.Values)
			{
				if (node == null) throw new InvalidOperationException($"Missing child for {nameof(EntityPack)}.");
				foreach (PackInstance instance in instances) builder.Add(CreatePreparedInstance(preparer, instance));
			}

			return builder.ToImmutable();
		}

		static PreparedInstance CreatePreparedInstance(ScenePreparer preparer, PackInstance source)
		{
			Node node = preparer.CreateOrGetNode(source.Pack);

			var pack = node.preparedPack;
			var extractor = node.swatchExtractor;

			if (pack != null && extractor != null) return new PreparedInstance(pack, extractor.Prepare(source.Swatch), source.InverseTransform);
			throw new InvalidOperationException($"Cannot create {nameof(PreparedInstance)} because the source {nameof(PreparedPack)} is missing.");
		}
	}
}