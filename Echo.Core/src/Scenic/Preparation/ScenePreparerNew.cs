using System;
using System.Collections.Generic;
using CodeHelpers;
using Echo.Core.Aggregation.Acceleration;
using Echo.Core.Scenic.Instancing;

namespace Echo.Core.Scenic.Preparation;

public sealed partial record ScenePreparerNew
{
	public ScenePreparerNew(Scene scene) => this.scene = scene;

	ScenePreparerNew(ScenePreparerNew source)
	{
		scene = source.scene;
		AcceleratorCreator = source.AcceleratorCreator;
		FragmentationThreshold = source.FragmentationThreshold;
		FragmentationMaxIteration = source.FragmentationMaxIteration;
	}

	readonly Scene scene;

	readonly Dictionary<EntityPack, Node> nodes = new();

	/// <summary>
	/// The <see cref="AcceleratorCreator"/> used for this <see cref="ScenePreparerNew"/>.
	/// </summary>
	public AcceleratorCreator AcceleratorCreator { get; init; }

	readonly float _fragmentationThreshold = 5.8f;
	readonly int _fragmentationMaxIteration = 3;

	/// <summary>
	/// How many times does the area of a triangle has to be over the average of all triangles to trigger a fragmentation.
	/// Fragmentation can cause the construction of better <see cref="Accelerator"/>, however it can also backfire.
	/// </summary>
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
	public int FragmentationMaxIteration
	{
		get => _fragmentationMaxIteration;
		init
		{
			if (_fragmentationMaxIteration >= 0) _fragmentationMaxIteration = value;
			else throw ExceptionHelper.Invalid(nameof(value), value, InvalidType.outOfBounds);
		}
	}

	public PreparedScene Prepare()
	{
		Root root = CreateOrGetRoot(scene);
		var queue = new Queue<Node>();

		queue.Enqueue(root);

		for (int i = 0; i < EntityPack.MaxLayer; i++)
		{
			
		}
		
		do
		{
			Node current = queue.Dequeue();

			foreach (EntityPack pack in current.InstancingPacks)
			{
				queue.Enqueue(CreateOrGetNode(pack));
			}
		}
		while (queue.Count > 0);

		return root.CreatePreparedScene(this);
	}

	Node CreateOrGetNode(EntityPack pack)
	{
		if (nodes.TryGetValue(pack, out Node node)) return node;

		node = new Node(pack);
		nodes.Add(pack, node);
		return node;
	}

	Root CreateOrGetRoot(EntityPack pack)
	{
		if (nodes.TryGetValue(pack, out Node node) && node is Root root) return root;

		root = new Root(pack);
		nodes[pack] = root;
		return root;
	}
}