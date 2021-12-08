using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using CodeHelpers.Pooling;
using EchoRenderer.Mathematics.Accelerators;
using EchoRenderer.Objects.GeometryObjects;
using EchoRenderer.Rendering.Profiles;

namespace EchoRenderer.Objects.Scenes
{
	public class ScenePresser
	{
		public ScenePresser(Scene scene, ScenePressProfile profile)
		{
			this.profile = profile;
			materials = new MaterialPresser();
			root = CreateNode(scene, null);

			threadId = Thread.CurrentThread.ManagedThreadId;

			PressPacks(root);
		}

		public readonly ScenePressProfile profile;
		public readonly MaterialPresser materials;
		public readonly Node root;

		public Dictionary<ObjectPack, Node>.KeyCollection UniquePacks => objectPacks.Keys;

		readonly int threadId;

		readonly Dictionary<ObjectPack, Node> objectPacks = new();
		readonly List<PressedInstance> packInstances = new();

		/// <summary>
		/// Creates or retrieves and returns the <see cref="PressedPack"/> for <paramref name="pack"/>.
		/// </summary>
		public PressedPack GetPressedPack(ObjectPack pack)
		{
			Node node = objectPacks.TryGetValue(pack);

			if (node == null) throw ExceptionHelper.Invalid(nameof(pack), pack, "is not linked in the input scene in any way");
			if (node.PressedPack == null) throw new Exception("Pack not pressed yet! Are you sure the pressing order is correct?");

			return node.PressedPack;
		}

		/// <summary>
		/// Returns a unique id for the newly created <paramref name="instance"/> and register it into this <see cref="ScenePresser"/>.
		/// NOTE: This method must be invoked on the same thread as the constructor of this <see cref="ScenePresser"/>!
		/// </summary>
		public uint RegisterPressedPackInstance(PressedInstance instance)
		{
			if (threadId != Thread.CurrentThread.ManagedThreadId) throw new Exception($"Invalid thread {threadId}!");

			packInstances.Add(instance);
			return (uint)(packInstances.Count - 1);
		}

		/// <summary>
		/// Retrieves a registered <see cref="PressedInstance"/> with
		/// <paramref name="id"/> from this <see cref="ScenePresser"/>.
		/// </summary>
		public PressedInstance GetPressedPackInstance(uint id) => packInstances[(int)id];

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
				if (child is not ObjectInstance instance || instance.ObjectPack == null) continue;

				Node childNode = CreateNode(instance.ObjectPack, node);

				if (!node.AddChild(childNode)) continue; //If we did not add, then the node already existed
				if (node.HasParent(childNode)) throw new Exception($"Recursive {nameof(ObjectPack)} instancing!");
			}

			return node;
		}

		void PressPacks(Node node)
		{
			//Head recursion to make sure that all children is pressed before the parent

			foreach (Node child in node) PressPacks(child);
			node.AssignPack(new PressedPack(this, node.objectPack));
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

			readonly HashSet<Node> parents = new();
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
			IEnumerator IEnumerable.            GetEnumerator() => GetEnumerator();
		}
	}
}