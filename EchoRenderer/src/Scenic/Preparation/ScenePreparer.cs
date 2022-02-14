using System;
using System.Collections;
using System.Collections.Generic;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using CodeHelpers.Pooling;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.Profiles;
using EchoRenderer.Scenic.Geometries;
using EchoRenderer.Scenic.Instancing;

namespace EchoRenderer.Scenic.Preparation;

public class ScenePreparer
{
	public ScenePreparer(Scene scene, ScenePrepareProfile profile)
	{
		this.profile = profile;
		root = CreateNode(scene, null);
	}

	public readonly ScenePrepareProfile profile;
	public readonly Node root; //This field can be made private once a better monitoring system is introduced

	readonly Dictionary<EntityPack, Node> entityPacks = new();
	readonly HashSet<Material> preparedMaterials = new();

	public Dictionary<EntityPack, Node>.KeyCollection UniquePacks => entityPacks.Keys;

	/// <summary>
	/// Prepares the entire scene.
	/// </summary>
	public void PrepareAll() => PreparePacks(root);

	/// <summary>
	/// Creates or retrieves and returns the <see cref="PreparedPack"/> for <paramref name="pack"/>.
	/// </summary>
	public PreparedPack GetPreparedPack(EntityPack pack)
	{
		Node node = entityPacks.TryGetValue(pack);

		if (node == null) throw ExceptionHelper.Invalid(nameof(pack), pack, "is not linked in the input scene in any way");
		if (node.PreparedPack == null) throw new Exception("Pack not prepared! Are you sure the preparing order is correct?");

		return node.PreparedPack;
	}

	/// <summary>
	/// If needed, prepares <paramref name="material"/> to be ready for rendering along with <see cref="Scene"/>.
	/// </summary>
	public void PrepareMaterial(Material material)
	{
		if (preparedMaterials.Add(material)) material.Prepare();
	}

	Node CreateNode(EntityPack pack, Node parent)
	{
		if (entityPacks.TryGetValue(pack, out Node node))
		{
			node.AddParent(parent);
			return node;
		}

		node = new Node(pack, parent);
		entityPacks.Add(pack, node);

		foreach (Entity child in pack.LoopChildren(true))
		{
			if (child is EntityPack) throw new Exception($"Cannot directly assign {child} as a child!");
			if (child is not PackInstance instance || instance.EntityPack == null) continue;

			Node childNode = CreateNode(instance.EntityPack, node);

			if (!node.AddChild(childNode)) continue; //If we did not add, then the node already existed
			if (node.HasParent(childNode)) throw new Exception($"Recursive {nameof(EntityPack)} instancing!");
		}

		return node;
	}

	void PreparePacks(Node node)
	{
		//Head recursion to make sure that all children are prepared before the parent

		foreach (Node child in node) PreparePacks(child);
		node.AssignPack(new PreparedPack(this, node.entityPack));
	}

	public class Node : IEnumerable<Node>
	{
		public Node(EntityPack entityPack, Node parent)
		{
			this.entityPack = entityPack;
			if (parent != null) parents.Add(parent);
		}

		public readonly EntityPack entityPack;
		public PreparedPack PreparedPack { get; private set; }

		public GeometryCounts InstancedCounts { get; private set; }
		public GeometryCounts UniqueCounts { get; private set; }

		readonly HashSet<Node> parents = new();
		readonly Dictionary<Node, uint> children = new(); //Maps child to the number of duplicated instances

		/// <summary>
		/// Tries to add <paramref name="child"/>, returns true if the child has not been added before.
		/// </summary>
		public bool AddChild(Node child)
		{
			uint count = children.TryGetValue(child);
			children[child] = count + 1;

			return count == 0; //TryGetValue defaults to zero if does not exist
		}

		/// <summary>
		/// Tries to add <paramref name="parent"/>, returns true if the parent has not been added before.
		/// </summary>
		public bool AddParent(Node parent) => parents.Add(parent);

		/// <summary>
		/// Expensive method, searches the entire parent inheritance tree for <paramref name="node"/>.
		/// Returns true if <paramref name="node"/> is either a direct or indirect parent of this node.
		/// NOTE: This also returns true if <paramref name="node"/> is exactly just this node.
		/// </summary>
		public bool HasParent(Node node)
		{
			if (node == this) return true;

			using var searched = CollectionPooler<Node>.hashSet.Fetch();

			return Search(this);

			bool Search(Node current)
			{
				foreach (Node parent in current.parents)
				{
					if (!searched.Target.Add(parent)) continue;
					if (parent == node || Search(parent)) return true;
				}

				return false;
			}
		}

		public void AssignPack(PreparedPack preparedPack)
		{
			Assert.IsNull(PreparedPack);
			PreparedPack = preparedPack;

			foreach ((Node child, uint number) in children)
			{
				InstancedCounts += child.InstancedCounts * number;
				UniqueCounts += child.UniqueCounts;
			}

			InstancedCounts += preparedPack.geometryCounts;
			UniqueCounts += preparedPack.geometryCounts;
		}

		Dictionary<Node, uint>.KeyCollection.Enumerator GetEnumerator() => children.Keys.GetEnumerator();

		IEnumerator<Node> IEnumerable<Node>.GetEnumerator() => GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}