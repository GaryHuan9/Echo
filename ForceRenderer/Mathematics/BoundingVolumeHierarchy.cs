using System;
using System.Collections.Generic;
using System.Linq;
using CodeHelpers;
using CodeHelpers.Collections;
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

			var listPooler = new CollectionPoolerBase<List<int>, int>(int.MaxValue);
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

				//Cuts so that the item at cutIndex is the beginning of the second half
				int cutIndex = 1;
				float minCost = float.MaxValue;

				for (int i = 1; i < indices.Count; i += Math.Max(1, indices.Count / 17))
				{
					//Calculate cut cost between i-1 and i
					float area0 = AxisAlignedBoundingBox.Construct(aabbs, indices, 0, i).Area;
					float area1 = AxisAlignedBoundingBox.Construct(aabbs, indices, i, indices.Count - i).Area;

					float cost = area0 * i + area1 * (indices.Count - i);
					if (cost >= minCost) break;

					minCost = cost;
					cutIndex = i;
				}

				//Recursively construct deeper layers
				int layerIndex = currentIndex++;

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
		public float GetIntersection(in Ray ray, out int token, out Float2 uv)
		{
			token = default;
			uv = default;

			ref Node node = ref nodes[0]; //The root node
			float hit = node.aabb.Intersect(ray);

			if (float.IsInfinity(hit)) return float.PositiveInfinity;

			float distance = float.PositiveInfinity;
			IntersectNode(ref node, ray, ref distance, ref token, ref uv);

			return distance;
		}

		void IntersectNode(ref Node node, in Ray ray, ref float distance, ref int token, ref Float2 uv)
		{
			if (node.IsLeaf)
			{
				//Now we finally calculate the real intersection
				float hit = pressed.GetIntersection(ray, node.token, out Float2 local);

				if (hit < distance)
				{
					distance = hit;
					token = node.token;
					uv = local;
				}

				return;
			}

			ref Node child0 = ref nodes[node.childIndex0];
			ref Node child1 = ref nodes[node.childIndex1];

			float hit0 = child0.aabb.Intersect(ray);
			float hit1 = child1.aabb.Intersect(ray);

			if (hit0 < hit1) //Orderly intersects the two children so that there is a higher chance of intersection on the first child
			{
				if (hit0 < distance) IntersectNode(ref child0, ray, ref distance, ref token, ref uv);
				if (hit1 < distance) IntersectNode(ref child1, ray, ref distance, ref token, ref uv);
			}
			else
			{
				if (hit1 < distance) IntersectNode(ref child1, ray, ref distance, ref token, ref uv);
				if (hit0 < distance) IntersectNode(ref child0, ray, ref distance, ref token, ref uv);
			}
		}

		readonly struct Node
		{
			/// <summary>
			/// Creates this node as a leaf node.
			/// </summary>
			public Node(AxisAlignedBoundingBox aabb, int token) : this()
			{
				this.token = token;
				this.aabb = aabb;
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