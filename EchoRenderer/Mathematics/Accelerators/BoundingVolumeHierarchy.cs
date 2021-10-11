using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using EchoRenderer.Mathematics.Intersections;

namespace EchoRenderer.Mathematics.Accelerators
{
	/// <summary>
	/// A binary hierarchical spacial partitioning acceleration structure.
	/// Works best with medium sized quantities of geometries and tokens.
	/// There must be more than one token and <see cref="AxisAlignedBoundingBox"/> to process.
	/// </summary>
	public class BoundingVolumeHierarchy : TraceAccelerator
	{
		public BoundingVolumeHierarchy(PressedPack pack, IReadOnlyList<AxisAlignedBoundingBox> aabbs, IReadOnlyList<uint> tokens) : base(pack, aabbs, tokens)
		{
			int[] indices = Enumerable.Range(0, aabbs.Count).ToArray();

			BranchBuilder builder = new BranchBuilder(aabbs);
			BranchBuilder.Node root = builder.Build(indices);

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

		public override int Hash
		{
			get
			{
				int hash = maxDepth;

				foreach (Node node in nodes) hash = (hash * 397) ^ node.GetHashCode();

				return hash;
			}
		}

		readonly Node[] nodes;
		readonly int maxDepth;

		public override void GetIntersection(ref HitQuery query)
		{
			if (nodes == null) return;

			if (nodes.Length == 1)
			{
				uint token = nodes[0].token; //If root is the only node/leaf
				pack.GetIntersection(ref query, token);
			}
			else
			{
				ref readonly Node root = ref nodes[0];
				float local = root.aabb.Intersect(query.ray);

				if (local < query.distance) Traverse(ref query);
			}
		}

		public override int GetIntersectionCost(in Ray ray, ref float distance)
		{
			if (nodes == null) return 0;

			ref readonly Node root = ref nodes[0];
			float hit = root.aabb.Intersect(ray);

			if (hit >= distance) return 1;
			return GetIntersectionCost(root, ray, ref distance) + 1;
		}

		public override unsafe int FillAABB(int depth, Span<AxisAlignedBoundingBox> span)
		{
			int length = 1 << (depth - 1);
			if (length > span.Length) throw new Exception($"{nameof(span)} is not large enough! Length: '{span.Length}'");

			int* stack0 = stackalloc int[length];
			int* stack1 = stackalloc int[length];

			int* next0 = stack0;
			int* next1 = stack1;

			*next0++ = 0; //Root at 0

			for (int i = 1; i < depth; i++)
			{
				while (next0 != stack0)
				{
					int index = *--next0;
					ref readonly Node node = ref nodes[index];

					if (node.IsLeaf) //If leaf then we just continue with this node
					{
						*next1++ = index;
					}
					else
					{
						*next1++ = node.children;
						*next1++ = node.children + 1;
					}
				}

				//Swap the two stacks
				int* next = next0;
				int* stack = stack0;

				next0 = next1;
				next1 = next;

				stack0 = stack1;
				stack1 = stack;
			}

			//Export results
			int current = 0;

			while (next0 != stack0)
			{
				ref readonly Node node = ref nodes[*--next0];
				span[current++] = node.aabb;
			}

			return current;
		}

		unsafe void Traverse(ref HitQuery query)
		{
			int* stack = stackalloc int[maxDepth];
			float* hits = stackalloc float[maxDepth];

			int* next = stack;

			*next++ = 1;  //The root's first children is always at one
			*hits++ = 0f; //stackalloc does not guarantee data to be zero, we have to manually assign it

			while (next != stack)
			{
				int index = *--next;
				if (*--hits >= query.distance) continue;

				ref readonly Node child0 = ref nodes[index];
				ref readonly Node child1 = ref nodes[index + 1];

				float hit0 = child0.aabb.Intersect(query.ray);
				float hit1 = child1.aabb.Intersect(query.ray);

				//Orderly intersects the two children so that there is a higher chance of intersection on the first child.
				//Although the order of leaf intersection is wrong, the performance is actually better than reversing to correct it.

				if (hit0 < hit1)
				{
					if (hit1 < query.distance)
					{
						if (child1.IsLeaf) pack.GetIntersection(ref query, child1.token);
						else
						{
							*next++ = child1.children;
							*hits++ = hit1;
						}
					}

					if (hit0 < query.distance)
					{
						if (child0.IsLeaf) pack.GetIntersection(ref query, child0.token);
						else
						{
							*next++ = child0.children;
							*hits++ = hit0;
						}
					}
				}
				else
				{
					if (hit0 < query.distance)
					{
						if (child0.IsLeaf) pack.GetIntersection(ref query, child0.token);
						else
						{
							*next++ = child0.children;
							*hits++ = hit0;
						}
					}

					if (hit1 < query.distance)
					{
						if (child1.IsLeaf) pack.GetIntersection(ref query, child1.token);
						else
						{
							*next++ = child1.children;
							*hits++ = hit1;
						}
					}
				}
			}
		}

		int GetIntersectionCost(in Node node, in Ray ray, ref float distance)
		{
			if (node.IsLeaf)
			{
				//Now we finally calculate the intersection cost on the leaf
				return pack.GetIntersectionCost(ray, ref distance, node.token);
			}

			ref Node child0 = ref nodes[node.children];
			ref Node child1 = ref nodes[node.children + 1];

			float hit0 = child0.aabb.Intersect(ray);
			float hit1 = child1.aabb.Intersect(ray);

			int cost = 2;

			if (hit0 < hit1) //Orderly intersects the two children so that there is a higher chance of intersection on the first child
			{
				if (hit0 < distance) cost += GetIntersectionCost(in child0, ray, ref distance);
				if (hit1 < distance) cost += GetIntersectionCost(in child1, ray, ref distance);
			}
			else
			{
				if (hit1 < distance) cost += GetIntersectionCost(in child1, ray, ref distance);
				if (hit0 < distance) cost += GetIntersectionCost(in child0, ray, ref distance);
			}

			return cost;
		}

		[StructLayout(LayoutKind.Explicit, Size = 32)] //Size must be under 32 bytes to fit two nodes in one cache line (64 bytes)
		readonly struct Node
		{
			Node(in AxisAlignedBoundingBox aabb, uint token, int children)
			{
				this.aabb = aabb; //AABB is assigned before the last two fields
				this.token = token;
				this.children = children;
			}

			[FieldOffset(0)] public readonly AxisAlignedBoundingBox aabb;

			//NOTE: the AABB is 28 bytes large, but its last 4 bytes are not used and only occupied for SIMD loading
			//So we can overlap the next four bytes onto the AABB and pay extra attention when first assigning the fields

			[FieldOffset(24)] public readonly uint token;   //Token will only be assigned if is leaf
			[FieldOffset(28)] public readonly int children; //Index of first child, second child is right after first

			public bool IsLeaf => children == 0;

			public static Node CreateLeaf(in AxisAlignedBoundingBox aabb, uint token) => new Node(aabb, token, 0);
			public static Node CreateNode(in AxisAlignedBoundingBox aabb, int children) => new Node(aabb, default, children);

			public override int GetHashCode()
			{
				unchecked
				{
					int hashCode = aabb.GetHashCode();
					hashCode = (hashCode * 397) ^ (int)token;
					hashCode = (hashCode * 397) ^ children;
					return hashCode;
				}
			}
		}
	}
}