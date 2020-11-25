using System;
using System.Collections.Generic;
using System.Linq;
using CodeHelpers;
using CodeHelpers.ObjectPooling;
using CodeHelpers.Vectors;
using ForceRenderer.Renderers;

namespace ForceRenderer.Mathematics
{
	public class BoundingVolumeHierarchy
	{
		public BoundingVolumeHierarchy(PressedScene pressed, IReadOnlyList<AxisAlignedBoundingBox> aabbs, IReadOnlyList<int> tokens)
		{
			if (aabbs.Count != tokens.Count) throw ExceptionHelper.Invalid(nameof(tokens), tokens, $"does not have a matching length with {nameof(aabbs)}");

			this.pressed = pressed;

			int currentIndex = 0;
			nodes = new Node[aabbs.Count * 2 - 1]; //The number of nodes can be predetermined

			var listPooler = new CollectionPoolerBase<List<int>, int>();
			ConstructLayer(Enumerable.Range(0, aabbs.Count).ToList());

			int ConstructLayer(List<int> indices) //Returns an int, the index of the layer constructed
			{
				if (indices.Count == 1) //If is leaf node
				{
					int leaf = indices[0];
					nodes[currentIndex] = new Node(aabbs[leaf], tokens[leaf]);

					return currentIndex++;
				}

				var aabb = AxisAlignedBoundingBox.Construct(aabbs, indices);
				int axis = aabb.extend.MaxIndex; //The index this layer is going to split at

				//Sorts the indices by splitting axis value. NOTE: That list is modified
				indices.Sort((index0, index1) => aabbs[index0].center[axis].CompareTo(aabbs[index1].center[axis]));

				//Use the medium for now; and although we do not need to sort the list for medium, we will need it sorted later
				int cutIndex = indices.Count / 2; //Cuts so that the item at cutIndex is the beginning of the second half

				int layerIndex = currentIndex++;

				//Recursively construct deeper layers
				var indices0 = listPooler.GetObject();
				var indices1 = listPooler.GetObject();

				for (int i = 0; i < cutIndex; i++) indices0.Add(indices[i]);
				for (int i = cutIndex; i < indices.Count; i++) indices1.Add(indices[i]);

				int childIndex0 = ConstructLayer(indices0);
				int childIndex1 = ConstructLayer(indices1);

				listPooler.ReleaseObject(indices0);
				listPooler.ReleaseObject(indices1);

				//Store layer
				nodes[layerIndex] = new Node(aabb, childIndex0, childIndex1);
				return layerIndex;
			}
		}

		public readonly PressedScene pressed;
		readonly Node[] nodes;

		/// <summary>
		/// Traverses and finds the closest intersection of <paramref name="ray"/> with this BVH.
		/// Returns the distance of ths intersection if found. <see cref="float.PositiveInfinity"/> otherwise.
		/// </summary>
		public float GetIntersection(in Ray ray, out int token)
		{

		}

		readonly struct Node
		{
			/// <summary>
			/// Creates this node as a leaf node.
			/// </summary>
			public Node(AxisAlignedBoundingBox aabb, int token) : this()
			{
				this.token = token;
				this.aabb = aabb; //TODO: Test if when traversing, whether it is faster to test this leaf aabb or not
			}

			/// <summary>
			/// Creates this node as a layer node.
			/// </summary>
			public Node(AxisAlignedBoundingBox aabb, int childIndex0, int childIndex1) : this()
			{
				this.aabb = aabb;

				this.childIndex0 = childIndex0;
				this.childIndex1 = childIndex1;
			}

			public readonly int token; //Token will only be assigned if is leaf
			public readonly AxisAlignedBoundingBox aabb;

			public readonly int childIndex0;
			public readonly int childIndex1;

			public bool IsLeaf => childIndex0 == 0;
		}
	}
}