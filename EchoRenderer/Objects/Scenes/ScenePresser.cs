using System;
using System.Collections;
using System.Collections.Generic;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using CodeHelpers.ObjectPooling;
using EchoRenderer.Mathematics.Intersections;

namespace EchoRenderer.Objects.Scenes
{
	public class ScenePresser
	{
		public ScenePresser(Scene scene)
		{
			root = CreateNode(scene, null);
			materials = new MaterialPresser();
		}

		public readonly MaterialPresser materials;
		public readonly Node root;

		public Dictionary<ObjectPack, Node>.KeyCollection UniquePacks => objectPacks.Keys;
		readonly Dictionary<ObjectPack, Node> objectPacks = new Dictionary<ObjectPack, Node>();

		public PressedPack GetPressedPack(ObjectPack pack)
		{
			Node node = objectPacks.TryGetValue(pack);

			if (node == null) throw ExceptionHelper.Invalid(nameof(pack), pack, "is not linked in the input scene in any way");
			if (node.PressedPack == null) throw new Exception("Pack not pressed yet! Are you sure the pressing order is correct?");

			return node.PressedPack;
		}

		public PressedPack PressPacks() => PressPacks(root);

		Node CreateNode(ObjectPack pack, Node parent)
		{
			if (objectPacks.TryGetValue(pack, out Node node))
			{
				node.AddParent(parent);
				return node;
			}

			node = new Node(pack, parent);
			objectPacks.Add(pack, node);

			foreach (Object child in pack.LoopChildren(true))
			{
				if (child is ObjectPack) throw new Exception($"Cannot directly assign {child} as a child!");
				if (child is not ObjectPackInstance instance || instance.ObjectPack == null) continue;

				Node childNode = CreateNode(instance.ObjectPack, node);

				if (!node.AddChild(childNode)) continue; //If we did not add, then the node already existed
				if (node.HasParent(childNode)) throw new Exception($"Recursive {nameof(ObjectPack)} instancing!");
			}

			return node;
		}

		PressedPack PressPacks(Node node)
		{
			foreach (Node child in node) PressPacks(child);
			node.AssignPack(new PressedPack(node.objectPack, this));

			//Head recursion to make sure that all children is pressed before the parent

			return node.PressedPack;
		}

		public class Node : IEnumerable<Node>
		{
			public Node(ObjectPack objectPack, Node parent)
			{
				this.objectPack = objectPack;
				if (parent != null) parents.Add(parent);
			}

			public readonly ObjectPack objectPack;
			public PressedPack PressedPack { get; private set; }

			public GeometryCounts InstancedCounts { get; private set; }
			public GeometryCounts UniqueCounts { get; private set; }

			readonly HashSet<Node> parents = new HashSet<Node>();
			readonly Dictionary<Node, int> children = new(); //Maps child to the number of duplicated instances

			/// <summary>
			/// Tries to add <paramref name="child"/>, returns true if the child has not been added before.
			/// </summary>
			public bool AddChild(Node child)
			{
				int number = children.TryGetValue(child);
				children[child] = number + 1;

				return number == 0; //TryGetValue defaults to zero if does not exist
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

			public void AssignPack(PressedPack pressedPack)
			{
				Assert.IsNull(PressedPack);
				PressedPack = pressedPack;

				foreach ((Node child, int number) in children)
				{
					InstancedCounts += child.InstancedCounts * number;
					UniqueCounts += child.UniqueCounts;
				}

				InstancedCounts += pressedPack.geometryCounts;
				UniqueCounts += pressedPack.geometryCounts;
			}

			Dictionary<Node, int>.KeyCollection.Enumerator GetEnumerator() => children.Keys.GetEnumerator();

			IEnumerator<Node> IEnumerable<Node>.GetEnumerator() => GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
	}
}