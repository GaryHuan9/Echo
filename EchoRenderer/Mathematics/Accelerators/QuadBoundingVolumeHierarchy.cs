using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using EchoRenderer.Mathematics.Intersections;

namespace EchoRenderer.Mathematics.Accelerators
{
	/// <summary>
	/// A four-way bounding volume hierarchy. There must be more than one token and <see cref="AxisAlignedBoundingBox"/> to process.
	/// </summary>
	public class QuadBoundingVolumeHierarchy : TraceAccelerator
	{
		public QuadBoundingVolumeHierarchy(PressedPack pack, IReadOnlyList<AxisAlignedBoundingBox> aabbs, IReadOnlyList<uint> tokens) : base(pack, aabbs, tokens)
		{
			if (tokens.Count <= 1) throw ExceptionHelper.Invalid(nameof(tokens.Count), tokens.Count, "does contain more than one token");

			int[] indices = Enumerable.Range(0, aabbs.Count).ToArray();

			BranchBuilder builder = new BranchBuilder(aabbs);
			BranchBuilder.Node root = builder.Build(indices);

			int count = 1;
			int index = 0;

			var buildRoot = new BuildNode(root, ref count);

			Assert.IsTrue(count > 1);

			nodes = new Node[count];
			nodes[0] = CreateNode(buildRoot, out maxDepth);

			Span<int> a;

			Node CreateNode(BuildNode node, out int depth)
			{
				Assert.IsNotNull(node.child);
				BuildNode current = node.child;

				Span<uint> children = stackalloc uint[4];
				int childIndex = 0;

				while (current != null)
				{
					if (current.child == null)
					{
						int nodeIndex = current.node.index;
						children[childIndex] = tokens[nodeIndex];
					}
					else
					{
						ref Node child = ref nodes[++index];
					}

					current = current.sibling;
					++childIndex;
				}

				current = current.sibling;

				if (current != null)
				{
					if (current.child == null) *children++ = 3;
					else
					{
						nodes[index] = CreateNode(current, out depth);
					}
				}


				current = current.sibling;

				if (node.chi) nodes[++index] = 

				if (node.IsLeaf)
				{
					depth = 1;
					return Node.CreateLeaf(node.aabb, tokens[node.index]);
				}

				// int children = index;
				index += 2;

				nodes[children] = CreateNode(node.child0, out int depth0);
				nodes[children + 1] = CreateNode(node.child1, out int depth1);

				depth = Math.Max(depth0, depth1) + 1;
				return Node.CreateNode(node.aabb, children);
			}
		}

		readonly Node[] nodes;
		readonly int maxDepth;

		public override int Hash { get; }

		public override void GetIntersection(ref HitQuery query)
		{
			throw new NotImplementedException();
		}

		public override int GetIntersectionCost(in Ray ray, ref float distance) => throw new NotImplementedException();

		public override int FillAABB(int depth, Span<AxisAlignedBoundingBox> span) => throw new NotImplementedException();

		[StructLayout(LayoutKind.Sequential, Size = 128)]
		readonly struct Node
		{
			public Node(BuildNode node)
			{
				Assert.IsNotNull(node.child);


				Unsafe.SkipInit(out padding);
			}

			public readonly AxisAlignedBoundingBox4 aabb;

			public readonly uint child0;
			public readonly uint child1;
			public readonly uint child2;
			public readonly uint child3;

			public readonly int axis0;
			public readonly int axis1;
			public readonly int axis2;

			readonly int padding;
		}

		class BuildNode
		{
			public BuildNode(BranchBuilder.Node node, ref int count, BuildNode sibling = null)
			{
				this.node = node;
				this.sibling = sibling;
				if (node.IsLeaf) return;

				var node0 = node.child0;
				var node1 = node.child1;

				//Swap to make sure that the non-leaf nodes go first
				if (node0.IsLeaf) CodeHelper.Swap(ref node0, ref node1);

				//Add children as a linked list
				//Note that the order is reversed

				if (!node1.IsLeaf)
				{
					Assert.IsFalse(node0.IsLeaf);

					AddChild(node1.child1, ref count, ref child);
					AddChild(node1.child0, ref count, ref child);
				}
				else AddChild(node1, ref count, ref child);

				if (!node0.IsLeaf)
				{
					AddChild(node0.child1, ref count, ref child);
					AddChild(node0.child0, ref count, ref child);
				}
				else AddChild(node0, ref count, ref child);

				static void AddChild(BranchBuilder.Node node, ref int count, ref BuildNode children)
				{
					children = new BuildNode(node, ref count, children);
					++count;
				}
			}

			public readonly BranchBuilder.Node node;

			public readonly BuildNode child;   //Linked list to the first child
			public readonly BuildNode sibling; //Reference to the next sibling node
		}
	}
}