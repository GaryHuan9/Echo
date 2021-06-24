using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeHelpers;

namespace EchoRenderer.Mathematics.Intersections
{
	public class BoundingVolumeHierarchy
	{
		public BoundingVolumeHierarchy(PressedPack pack, IReadOnlyList<AxisAlignedBoundingBox> aabbs, IReadOnlyList<uint> tokens)
		{
			this.pack = pack;

			if (aabbs.Count != tokens.Count) throw ExceptionHelper.Invalid(nameof(tokens), tokens, $"does not have a matching length with {nameof(aabbs)}");
			if (aabbs.Count == 0) return;

			int[] indices = Enumerable.Range(0, aabbs.Count).ToArray();

			//Parallel building reduces build time by about 4 folds on very large scenes
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

		readonly PressedPack pack;
		readonly Node[] nodes;
		readonly int maxDepth;

		/// <summary>
		/// Traverses and finds the closest intersection of <paramref name="ray"/> with this BVH.
		/// The intersection is recorded on <paramref name="hit"/>, and only only intersections
		/// that are closer than the initial <paramref name="hit.distance"/> value are tested.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void GetIntersection(in Ray ray, ref Hit hit)
		{
			if (nodes == null) return;

			if (nodes.Length == 1)
			{
				uint token = nodes[0].token; //If root is the only node/leaf
				pack.GetIntersection(ray, ref hit, token);
			}
			else
			{
				ref readonly Node root = ref nodes[0];
				float local = root.aabb.Intersect(ray);

				if (local < hit.distance) Traverse(ray, ref hit);
			}
		}

		/// <summary>
		/// Traverses and finds the closest intersection of <paramref name="ray"/> with this BVH.
		/// If a closer intersection distance is found, it will replace <paramref name="distance"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void GetIntersection(in Ray ray, ref float distance)
		{
			if (nodes == null) return;

			if (nodes.Length == 1)
			{
				uint token = nodes[0].token; //If root is the only node/leaf
				pack.GetIntersection(ray, ref distance, token);
			}
			else
			{
				ref readonly Node root = ref nodes[0];
				float local = root.aabb.Intersect(ray);

				if (local < distance) Traverse(ray, ref distance);
			}
		}

		/// <summary>
		/// Returns the number of AABB intersection calculated before a result it determined.
		/// </summary>
		public int GetIntersectionCost(in Ray ray, ref float distance)
		{
			if (nodes == null) return 0;

			ref readonly Node root = ref nodes[0];
			float hit = root.aabb.Intersect(ray);

			if (hit >= distance) return 1;
			return GetIntersectionCost(root, ray, ref distance) + 1;
		}

		/// <summary>
		/// Fills <paramref name="span"/> with the aabbs of nodes in this bvh at <paramref name="depth"/>.
		/// The root node has a <paramref name="depth"/> of 1. Returns the actual length of <paramref name="span"/>
		/// used to store the aabbs. NOTE: <paramref name="span"/> should not be shorter than 2 ^ (depth - 1).
		/// </summary>
		public unsafe int FillAABB(int depth, Span<AxisAlignedBoundingBox> span)
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

		/// <summary>
		/// Computes and returns a unique hash value for this entire <see cref="BoundingVolumeHierarchy"/>.
		/// This method can be slow on large BVHs. Can be used to compare BVH construction between runtimes.
		/// </summary>
		public int ComputeHash()
		{
			int hash = maxDepth;

			foreach (Node node in nodes) hash = (hash * 397) ^ node.GetHashCode();

			return hash;
		}

		unsafe void Traverse(in Ray ray, ref Hit hit)
		{
			int* stack = stackalloc int[maxDepth];
			float* hits = stackalloc float[maxDepth];

			int* next = stack;

			*next++ = 1;  //The root's first children is always at one
			*hits++ = 0f; //stackalloc does not guarantee data to be zero, we have to manually assign it

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
						if (child1.IsLeaf) pack.GetIntersection(ray, ref hit, child1.token);
						else
						{
							*next++ = child1.children;
							*hits++ = hit1;
						}
					}

					if (hit0 < hit.distance)
					{
						if (child0.IsLeaf) pack.GetIntersection(ray, ref hit, child0.token);
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
						if (child0.IsLeaf) pack.GetIntersection(ray, ref hit, child0.token);
						else
						{
							*next++ = child0.children;
							*hits++ = hit0;
						}
					}

					if (hit1 < hit.distance)
					{
						if (child1.IsLeaf) pack.GetIntersection(ray, ref hit, child1.token);
						else
						{
							*next++ = child1.children;
							*hits++ = hit1;
						}
					}
				}
			}
		}

		unsafe void Traverse(in Ray ray, ref float distance)
		{
			int* stack = stackalloc int[maxDepth];
			float* hits = stackalloc float[maxDepth];

			int* next = stack;

			*next++ = 1;  //The root's first children is always at one
			*hits++ = 0f; //stackalloc does not guarantee data to be zero, we have to manually assign it

			while (next != stack)
			{
				int index = *--next;
				if (*--hits >= distance) continue;

				ref readonly Node child0 = ref nodes[index];
				ref readonly Node child1 = ref nodes[index + 1];

				float hit0 = child0.aabb.Intersect(ray);
				float hit1 = child1.aabb.Intersect(ray);

				//Orderly intersects the two children so that there is a higher chance of intersection on the first child.
				//Although the order of leaf intersection is wrong, the performance is actually better than reversing to correct it.

				if (hit0 < hit1)
				{
					if (hit1 < distance)
					{
						if (child1.IsLeaf) pack.GetIntersection(ray, ref distance, child1.token);
						else
						{
							*next++ = child1.children;
							*hits++ = hit1;
						}
					}

					if (hit0 < distance)
					{
						if (child0.IsLeaf) pack.GetIntersection(ray, ref distance, child0.token);
						else
						{
							*next++ = child0.children;
							*hits++ = hit0;
						}
					}
				}
				else
				{
					if (hit0 < distance)
					{
						if (child0.IsLeaf) pack.GetIntersection(ray, ref distance, child0.token);
						else
						{
							*next++ = child0.children;
							*hits++ = hit0;
						}
					}

					if (hit1 < distance)
					{
						if (child1.IsLeaf) pack.GetIntersection(ray, ref distance, child1.token);
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