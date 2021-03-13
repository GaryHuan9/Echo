using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CodeHelpers;
using CodeHelpers.Mathematics;
using ForceRenderer.Rendering;

namespace ForceRenderer.Mathematics
{
	//AMD Ryzen 3900x: 1.7 million triangles at 23 million intersections per second

	public class BoundingVolumeHierarchy
	{
		public BoundingVolumeHierarchy(PressedScene pressed, IReadOnlyList<AxisAlignedBoundingBox> aabbs, IReadOnlyList<int> tokens)
		{
			if (aabbs.Count != tokens.Count) throw ExceptionHelper.Invalid(nameof(tokens), tokens, $"does not have a matching length with {nameof(aabbs)}");

			this.pressed = pressed;
			int nodeIndex = 1;

			MinMax3[] cutTailVolumes = new MinMax3[aabbs.Count]; //Array used to calculate SAH

			var rootIndices = Enumerable.Range(0, aabbs.Count).ToList();
			var rootAABB = AxisAlignedBoundingBox.Construct(aabbs);

			if (aabbs.Count == 0) return;

			nodes = new Node[aabbs.Count * 2 - 1]; //The number of nodes can be predetermined
			nodes[0] = ConstructNode(rootIndices, rootAABB, -1);

			maxDepth = GetMaxDepth(nodes[0]);

			Node ConstructNode(List<int> indices, AxisAlignedBoundingBox aabb, int parentAxis)
			{
				if (indices.Count == 1) //If is leaf node
				{
					int leaf = indices[0];
					return Node.CreateLeaf(aabbs[leaf], tokens[leaf]);
				}

				int axis = aabb.extend.MaxIndex; //The index this layer is going to split at

				//Sorts the indices by splitting axis if needed. NOTE that the list is modified
				if (axis != parentAxis) indices.Sort((index0, index1) => aabbs[index0].center[axis].CompareTo(aabbs[index1].center[axis]));

				//Calculate surface area heuristics and find optimal cut index
				MinMax3 cutHeadVolume = new MinMax3(aabbs[indices[0]]);
				MinMax3 cutTailVolume = new MinMax3(aabbs[indices[^1]]);

				for (int i = indices.Count - 2; i >= 0; i--)
				{
					cutTailVolumes[i + 1] = cutTailVolume;
					cutTailVolume = cutTailVolume.Encapsulate(aabbs[indices[i]]);
				}

				MinMax3 minCutHeadVolume = default;
				MinMax3 minCutTailVolume = default;

				float minCost = float.MaxValue;
				int minIndex = -1;
				float minTailCost = -1f;

				for (int i = 1; i < indices.Count; i++)
				{
					cutTailVolume = cutTailVolumes[i];

					float tailCost = cutTailVolume.Area * (indices.Count - i);
					float cost = cutHeadVolume.Area * i + tailCost;

					if (cost < minCost)
					{
						minCost = cost;
						minIndex = i;
						minTailCost = tailCost;

						minCutHeadVolume = cutHeadVolume;
						minCutTailVolume = cutTailVolume;
					}

					cutHeadVolume = cutHeadVolume.Encapsulate(aabbs[indices[i]]);
				}

				//Recursively construct deeper layers
				List<int> indicesHead = new List<int>(minIndex);
				List<int> indicesTail = new List<int>(indices.Count - minIndex);

				for (int i = 0; i < minIndex; i++) indicesHead.Add(indices[i]);
				for (int i = minIndex; i < indices.Count; i++) indicesTail.Add(indices[i]);

				if (minTailCost * 2f > minCost) //Orders the children so that the one with the higher SAH cost is first
				{
					CodeHelper.Swap(ref indicesHead, ref indicesTail);
					CodeHelper.Swap(ref minCutHeadVolume, ref minCutTailVolume);
				}

				int children = nodeIndex;
				nodeIndex += 2;

				nodes[children] = ConstructNode(indicesHead, minCutHeadVolume.AABB, axis);
				nodes[children + 1] = ConstructNode(indicesTail, minCutTailVolume.AABB, axis);

				return Node.CreateNode(aabb, children);
			}

			int GetMaxDepth(in Node node)
			{
				if (node.IsLeaf) return 1;

				int child0 = GetMaxDepth(nodes[node.children]);
				int child1 = GetMaxDepth(nodes[node.children + 1]);

				return Math.Max(child0, child1) + 1;
			}
		}

		readonly PressedScene pressed;
		readonly Node[] nodes;
		readonly int maxDepth;

		/// <summary>
		/// Traverses and finds the closest intersection of <paramref name="ray"/> with this BVH.
		/// Returns the intersection hit if found. Otherwise a <see cref="Hit"/> with <see cref="float.PositiveInfinity"/> distance.
		/// </summary>
		public Hit GetIntersection(in Ray ray)
		{
			if (nodes == null) return new Hit(float.PositiveInfinity);

			if (nodes.Length == 1)
			{
				int token = nodes[0].token; //If root is the only node/leaf
				return new Hit(pressed.Intersect(ray, token, out Float2 uv), token, uv);
			}

			ref readonly Node root = ref nodes[0];
			float first = root.aabb.Intersect(ray);

			Hit hit = new Hit(float.PositiveInfinity);
			if (first >= hit.distance) return hit;

			GetIntersection(ray, ref hit);
			return hit;
		}

		unsafe void GetIntersection(in Ray ray, ref Hit hit)
		{
			int* stack = stackalloc int[maxDepth];
			float* hits = stackalloc float[maxDepth];

			int* next = stack;
			*next++ = 1; //The root's first children is always at one

			while (next != stack)
			{
				int index = *--next;
				if (*--hits >= hit.distance) continue;

				ref readonly Node child0 = ref nodes[index];
				ref readonly Node child1 = ref nodes[index + 1];

				float hit0 = child0.aabb.Intersect(ray);
				float hit1 = child1.aabb.Intersect(ray);

				//Orderly intersects the two children so that there is a higher chance of intersection on the first child.
				//Although the order of leaf intersection is wrong, the performance is actually better than reversing to correct it.

				if (hit0 < hit1)
				{
					if (hit1 < hit.distance)
					{
						if (child1.IsLeaf)
						{
							float distance = pressed.Intersect(ray, child1.token, out Float2 uv);
							if (distance < hit.distance) hit = new Hit(distance, child1.token, uv);
						}
						else
						{
							*next++ = child1.children;
							*hits++ = hit1;
						}
					}

					if (hit0 < hit.distance)
					{
						if (child0.IsLeaf)
						{
							float distance = pressed.Intersect(ray, child0.token, out Float2 uv);
							if (distance < hit.distance) hit = new Hit(distance, child0.token, uv);
						}
						else
						{
							*next++ = child0.children;
							*hits++ = hit0;
						}
					}
				}
				else
				{
					if (hit0 < hit.distance)
					{
						if (child0.IsLeaf)
						{
							float distance = pressed.Intersect(ray, child0.token, out Float2 uv);
							if (distance < hit.distance) hit = new Hit(distance, child0.token, uv);
						}
						else
						{
							*next++ = child0.children;
							*hits++ = hit0;
						}
					}

					if (hit1 < hit.distance)
					{
						if (child1.IsLeaf)
						{
							float distance = pressed.Intersect(ray, child1.token, out Float2 uv);
							if (distance < hit.distance) hit = new Hit(distance, child1.token, uv);
						}
						else
						{
							*next++ = child1.children;
							*hits++ = hit1;
						}
					}
				}
			}
		}

		/// <summary>
		/// Returns the number of AABB intersection calculated before a result it determined.
		/// </summary>
		public int GetIntersectionCost(in Ray ray)
		{
			if (nodes == null) return 0;

			ref readonly Node root = ref nodes[0];
			float hit = root.aabb.Intersect(ray);

			if (float.IsPositiveInfinity(hit)) return 1;

			hit = float.PositiveInfinity;
			return GetIntersectionCost(root, ray, ref hit) + 1;
		}

		int GetIntersectionCost(in Node node, in Ray ray, ref float hit)
		{
			if (node.IsLeaf)
			{
				//Now we finally calculate the real intersection
				hit = Math.Min(pressed.Intersect(ray, node.token, out Float2 _), hit);
				return 0;
			}

			ref Node child0 = ref nodes[node.children];
			ref Node child1 = ref nodes[node.children + 1];

			float hit0 = child0.aabb.Intersect(ray);
			float hit1 = child1.aabb.Intersect(ray);

			int cost = 2;

			if (hit0 < hit1) //Orderly intersects the two children so that there is a higher chance of intersection on the first child
			{
				if (hit0 < hit) cost += GetIntersectionCost(in child0, ray, ref hit);
				if (hit1 < hit) cost += GetIntersectionCost(in child1, ray, ref hit);
			}
			else
			{
				if (hit1 < hit) cost += GetIntersectionCost(in child1, ray, ref hit);
				if (hit0 < hit) cost += GetIntersectionCost(in child0, ray, ref hit);
			}

			return cost;
		}

		[StructLayout(LayoutKind.Explicit, Size = 32)] //Size must be under 32 bytes to fit two nodes in one cache line (64 bytes)
		readonly struct Node
		{
			Node(in AxisAlignedBoundingBox aabb, int token, int children)
			{
				this.aabb = aabb; //AABB is assigned before the last two fields
				this.token = token;
				this.children = children;
			}

			[FieldOffset(0)]
			public readonly AxisAlignedBoundingBox aabb;

			//NOTE: the AABB is 28 bytes large, but its last 4 bytes are not used and only occupied for SIMD loading
			//So we can overlap the next four bytes onto the AABB and pay extra attention when first assigning the fields

			[FieldOffset(24)] public readonly int token;    //Token will only be assigned if is leaf
			[FieldOffset(28)] public readonly int children; //Index of first child, second child is right after first

			public bool IsLeaf => children == 0;

			public static Node CreateLeaf(in AxisAlignedBoundingBox aabb, int token) => new Node(aabb, token, 0);
			public static Node CreateNode(in AxisAlignedBoundingBox aabb, int children) => new Node(aabb, 0, children);
		}

		readonly struct MinMax3
		{
			public MinMax3(AxisAlignedBoundingBox aabb)
			{
				min = aabb.Min;
				max = aabb.Max;
			}

			public MinMax3(Float3 min, Float3 max)
			{
				this.min = min;
				this.max = max;
			}

			public readonly Float3 min;
			public readonly Float3 max;

			public float Area
			{
				get
				{
					Float3 size = max - min;
					return size.x * size.y + size.x * size.z + size.y * size.z;
				}
			}

			public AxisAlignedBoundingBox AABB
			{
				get
				{
					Float3 extend = (max - min) / 2f;
					return new AxisAlignedBoundingBox(min + extend, extend);
				}
			}

			public MinMax3 Encapsulate(AxisAlignedBoundingBox aabb) => new MinMax3(min.Min(aabb.Min), max.Max(aabb.Max));
		}

		// AxisAlignedBoundingBox[] aabbs =
		// {
		// 	new AxisAlignedBoundingBox(Float3.up, Float3.half),
		// 	new AxisAlignedBoundingBox(Float3.one * 2, Float3.half),
		// 	new AxisAlignedBoundingBox(Float3.right * 2, Float3.half),
		// 	new AxisAlignedBoundingBox(Float3.down * 2, Float3.half)
		// };
		//
		// var bvh = new BoundingVolumeHierarchy(null, aabbs, Enumerable.Range(0, aabbs.Length).ToList());
		//
		// var aabb = new AxisAlignedBoundingBox(Float3.zero, new Float3(0.5f, 0f, 0.5f));
		// Ray ray = new Ray(Float3.up, Float3.down);
		//
		// DebugHelper.Log(aabb.Intersect(ray));
	}

}