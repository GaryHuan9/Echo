using System;
using System.Collections.Generic;
using Echo.Core.Scenic.Instancing;

namespace Echo.Core.Scenic.Preparation;

public partial class ScenePreparerNew
{
	public ScenePreparerNew(Scene scene)
	{
		this.scene = scene;
	}

	readonly Scene scene;

	readonly Dictionary<EntityPack, Node> nodes = new();

	public PreparedScene Prepare()
	{
		throw new NotImplementedException();
	}

	Node Prepare(EntityPack pack)
	{
		if (nodes.TryGetValue(pack, out Node node)) return node;

		node = new Node(this, pack);
		nodes.Add(pack, node);
		return node;
	}
}