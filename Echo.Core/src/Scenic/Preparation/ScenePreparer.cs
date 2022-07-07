using System;
using System.Collections.Generic;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using Echo.Core.Aggregation.Acceleration;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Hierarchies;

namespace Echo.Core.Scenic.Preparation;

/// <summary>
/// An object used to create a <see cref="PreparedScene"/> from a <see cref="Scene"/>.
/// </summary>
public sealed partial record ScenePreparer
{
	public ScenePreparer(Scene scene) => this.scene = scene;

	ScenePreparer(ScenePreparer source)
	{
		scene = source.scene;
		AcceleratorCreator = source.AcceleratorCreator;
		FragmentationThreshold = source.FragmentationThreshold;
		FragmentationMaxIteration = source.FragmentationMaxIteration;
	}

	readonly Scene scene;

	readonly Dictionary<EntityPack, Node> nodes = new();
	readonly HashSet<Material> preparedMaterials = new();

	/// <summary>
	/// The <see cref="AcceleratorCreator"/> used for this <see cref="ScenePreparer"/>.
	/// </summary>
	public AcceleratorCreator AcceleratorCreator { get; init; }

	readonly float _fragmentationThreshold = 5.8f;
	readonly int _fragmentationMaxIteration = 3;

	/// <summary>
	/// How many times does the area of a triangle has to be over the average of all triangles to trigger a fragmentation.
	/// Fragmentation can cause the construction of better <see cref="Accelerator"/>, however it can also backfire.
	/// </summary>
	/// <remarks>Currently unused.</remarks>
	public float FragmentationThreshold
	{
		get => _fragmentationThreshold;
		init
		{
			if (_fragmentationThreshold >= 1f) _fragmentationThreshold = value;
			else throw ExceptionHelper.Invalid(nameof(value), value, InvalidType.outOfBounds);
		}
	}

	/// <summary>
	/// The maximum number of fragmentation that can happen to one source triangle.
	/// Note that we can completely disable fragmentation by setting this value to 0.
	/// </summary>
	/// <remarks>Currently unused.</remarks>
	public int FragmentationMaxIteration
	{
		get => _fragmentationMaxIteration;
		init
		{
			if (_fragmentationMaxIteration >= 0) _fragmentationMaxIteration = value;
			else throw ExceptionHelper.Invalid(nameof(value), value, InvalidType.outOfBounds);
		}
	}

	/// <summary>
	/// Creates a <see cref="PreparedScene"/>.
	/// </summary>
	/// <returns>The prepared version of the initially input <see cref="Scene"/>.</returns>
	public PreparedScene Prepare()
	{
		Node root = CreateOrGetNode(scene);
		CreateChildren(root, TokenHierarchy.MaxLayer);

		var visited = new HashSet<Node>();

		foreach (EntityPack pack in root.InstancingPacks)
		{
			Node child = nodes[pack];
			if (visited.Add(child)) CreatePrepared(child);
		}

		return root.CreatePreparedScene(this);

		// ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
		void CreateChildren(Node node, int budget)
		{
			if (budget < 0) throw new Exception("Maximum instancing layer exceeded!");

			foreach (EntityPack pack in node.InstancingPacks)
			{
				Node child = CreateOrGetNode(pack);
				CreateChildren(child, budget - 1);
				node.AddChild(child);
			}
		}

		void CreatePrepared(Node node)
		{
			foreach (EntityPack pack in node.InstancingPacks)
			{
				Node child = nodes[pack];
				if (visited.Add(child)) CreatePrepared(child);
			}

			Assert.IsFalse(visited.Contains(node));
			node.CreatePreparedPack(this);
		}
	}

	/// <summary>
	/// Prepares a <see cref="Material"/>.
	/// </summary>
	/// <param name="material">The material to be prepared by this <see cref="ScenePreparer"/>.</param>
	public void Prepare(Material material)
	{
		if (preparedMaterials.Add(material)) material.Prepare();
	}

	Node CreateOrGetNode(EntityPack pack)
	{
		if (nodes.TryGetValue(pack, out Node node)) return node;

		node = new Node(pack);
		nodes.Add(pack, node);
		return node;
	}
}