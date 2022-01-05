using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using CodeHelpers.Pooling;
using EchoRenderer.Objects.GeometryObjects;
using EchoRenderer.Objects.Preparation;
using EchoRenderer.Rendering.Profiles;

namespace EchoRenderer.Objects.Scenes
{
	public class ScenePreparer
	{
		public ScenePreparer(Scene scene, ScenePrepareProfile profile)
		{
			this.profile = profile;
			materials = new MaterialPreparer();
			root = CreateNode(scene, null);

			threadId = Thread.CurrentThread.ManagedThreadId;

			PreparePacks(root);
		}

		public readonly ScenePrepareProfile profile;
		public readonly MaterialPreparer materials;
		public readonly Node root;

		public Dictionary<ObjectPack, Node>.KeyCollection UniquePacks => objectPacks.Keys;

		readonly int threadId;

		readonly Dictionary<ObjectPack, Node> objectPacks = new();
		readonly List<PreparedInstance> packInstances = new();

		/// <summary>
		/// Returns a unique id for the newly created <paramref name="instance"/> and register it into this <see cref="ScenePreparer"/>.
		/// NOTE: This method must be invoked on the same thread as the constructor of this <see cref="ScenePreparer"/>!
		/// </summary>
		public uint RegisterPreparedInstance(PreparedInstance instance)
		{
			if (threadId != Thread.CurrentThread.ManagedThreadId) throw new Exception($"Invalid thread {threadId}!");

			packInstances.Add(instance);
			return (uint)(packInstances.Count - 1);
		}

		/// <summary>
		/// Creates or retrieves and returns the <see cref="PreparedPack"/> for <paramref name="pack"/>.
		/// </summary>
		public PreparedPack GetPreparedPack(ObjectPack pack)
		{
			Node node = objectPacks.TryGetValue(pack);

			if (node == null) throw ExceptionHelper.Invalid(nameof(pack), pack, "is not linked in the input scene in any way");
			if (node.PreparedPack == null) throw new Exception("Pack not prepared! Are you sure the preparing order is correct?");

			return node.PreparedPack;
		}

		/// <summary>
		/// Retrieves a registered <see cref="PreparedInstance"/> with
		/// <paramref name="id"/> from this <see cref="ScenePreparer"/>.
		/// </summary>
		public PreparedInstance GetPreparedInstance(uint id) => packInstances[(int)id];

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

		void PreparePacks(Node node)
		{
			//Head recursion to make sure that all children are prepared before the parent

			foreach (Node child in node) PreparePacks(child);
			node.AssignPack(new PreparedPack(this, node.objectPack));
		}

		public class Node : IEnumerable<Node>
		{
			public Node(ObjectPack objectPack, Node parent)
			{
				this.objectPack = objectPack;
				if (parent != null) parents.Add(parent);
			}

			public readonly ObjectPack objectPack;
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
			IEnumerator IEnumerable.            GetEnumerator() => GetEnumerator();
		}
	}
}