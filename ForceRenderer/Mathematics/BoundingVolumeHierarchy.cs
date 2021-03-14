using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Mathematics;
using ForceRenderer.Rendering;

namespace ForceRenderer.Mathematics
{
	public class BoundingVolumeHierarchy
	{
		public BoundingVolumeHierarchy(PressedScene pressed, IReadOnlyList<AxisAlignedBoundingBox> aabbs, IReadOnlyList<int> tokens)
		{
			this.pressed = pressed;

			if (aabbs.Count != tokens.Count) throw ExceptionHelper.Invalid(nameof(tokens), tokens, $"does not have a matching length with {nameof(aabbs)}");
			if (aabbs.Count == 0) return;

			int parallel = MathF.Log2(Environment.ProcessorCount).Ceil() + 1; //How many layers of parallel processes to build the bvh
			AxisAlignedBoundingBox aabb = new AxisAlignedBoundingBox(aabbs);  //Parallel building reduced build time by about 4 folds on large scenes

			BranchBuilder builder = new BranchBuilder(aabbs, Enumerable.Range(0, aabbs.Count).ToArray(), aabb);
			BranchBuilder.Node root = builder.Build(parallel); //NOTE: parallel is the number/depth of layers, not the number of processes

			int index = 1;

			nodes = new Node[builder.NodeCount];
			nodes[0] = CreateNode(root, out maxDepth);

			Node CreateNode(BranchBuilder.Node node, out int depth)
			{
				if (node.IsLeaf)
				{
					depth = 1;
					return Node.CreateLeaf(node.aabb, tokens[node.index]);
				}

				int children = index;
				index += 2;

				nodes[children] = CreateNode(node.child0, out int depth0);
				nodes[children + 1] = CreateNode(node.child1, out int depth1);

				depth = Math.Max(depth0, depth1) + 1;
				return Node.CreateNode(node.aabb, children);
			}
		}

		public BoundingVolumeHierarchy(PressedScene pressed, IReadOnlyList<AxisAlignedBoundingBox> aabbs, IReadOnlyList<int> tokens, bool no)
		{
			this.pressed = pressed;

			if (aabbs.Count != tokens.Count) throw ExceptionHelper.Invalid(nameof(tokens), tokens, $"does not have a matching length with {nameof(aabbs)}");
			if (aabbs.Count == 0) return;

			BinaryHeap<BuildNode> source = new BinaryHeap<BuildNode>(aabbs.Count);

			for (int i = 0; i < aabbs.Count; i++)
			{
				var node = new BuildNode(tokens[i], aabbs[i]);
				source.Enqueue(node, node.priority);
			}

			while (source.Count > 1)
			{
				BuildNode target = source.Dequeue();
				BuildNode other = null;

				foreach (BuildNode node in source)
				{

				}
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

		class BuildNode
		{
			public BuildNode(BuildNode child0, BuildNode child1, AxisAlignedBoundingBox aabb)
			{
				this.child0 = child0;
				this.child1 = child1;
				this.aabb = aabb;

				count = child0.count + child1.count + 1;
				priority = Scalars.SingleToInt32Bits(aabb.Area);
			}

			public BuildNode(int token, AxisAlignedBoundingBox aabb)
			{
				this.token = token;
				this.aabb = aabb;

				count = 1;
				priority = Scalars.SingleToInt32Bits(aabb.Area);
			}

			public readonly BuildNode child0;
			public readonly BuildNode child1;

			public readonly int token;
			public readonly int count;

			public readonly AxisAlignedBoundingBox aabb;
			public readonly int priority;
		}
	}
}