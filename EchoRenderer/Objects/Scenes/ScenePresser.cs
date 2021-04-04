using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Objects.Scenes
{
	public class ScenePresser
	{
		public ScenePresser(Scene scene)
		{
			root = CreateNode(scene, null);
			materials = new Materials(this);
		}

		public readonly Materials materials;
		public readonly Node root;

		readonly Dictionary<Material, int> materialTokens = new Dictionary<Material, int>();
		readonly Dictionary<ObjectPack, Node> objectPacks = new Dictionary<ObjectPack, Node>();

		public int GetMaterialToken(Material material)
		{
			if (material is Invisible) return -1; //Negative token used to omit invisible materials

			if (!materialTokens.TryGetValue(material, out int materialToken))
			{
				materialToken = materialTokens.Count;
				materialTokens.Add(material, materialToken);
			}

			return materialToken;
		}

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
			if (objectPacks.TryGetValue(pack, out Node node)) return node;

			node = new Node(pack, parent);
			objectPacks.Add(pack, node);

			foreach (Object child in pack.LoopChildren(true))
			{
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

		public class Materials
		{
			public Materials(ScenePresser presser) => this.presser = presser;

			readonly ScenePresser presser;
			Material[] materials;

			public int Length => materials.Length;

			public Material this[int index] => materials[index] ?? throw new Exception($"{nameof(Materials)} not pressed!");

			public void Press()
			{
				if (materials != null) throw new Exception($"{nameof(Materials)} already pressed!");

				materials = (from pair in presser.materialTokens
							 orderby pair.Value
							 select pair.Key).ToArray();

				for (int i = 0; i < materials.Length; i++) materials[i].Press();
			}
		}

		public class Node : IEnumerable<Node>
		{
			public Node(ObjectPack objectPack, Node parent)
			{
				this.objectPack = objectPack;
				this.parent = parent;
			}

			public readonly ObjectPack objectPack;
			public PressedPack PressedPack { get; private set; }

			public GeometryCounts InstancedCounts { get; private set; }
			public GeometryCounts UniqueCounts { get; private set; }

			readonly Node parent;
			readonly Dictionary<Node, int> children = new(); //Maps child to the number of duplicated instances

			/// <summary>
			/// Tries to add <paramref name="child"/>, returns if the child is unique.
			/// </summary>
			public bool AddChild(Node child)
			{
				int number = children.TryGetValue(child);
				children[child] = number + 1;

				return number == 0; //TryGetValue defaults to zero if does not exist
			}

			public bool HasParent(Node node)
			{
				Node current = parent;

				while (current != null)
				{
					if (current == node) return true;
					current = current.parent;
				}

				return false;
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