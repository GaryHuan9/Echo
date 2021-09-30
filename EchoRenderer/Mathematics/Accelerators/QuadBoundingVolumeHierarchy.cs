using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
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
			var buildRoot = new BuildNode(root, ref count);

			int nodeIndex = 0;

			Assert.IsTrue(count > 1);
			nodes = new Node[count];
			nodes[0] = CreateNode(buildRoot, out int maxDepth);

			stackSize = maxDepth * 3;

			Node CreateNode(BuildNode buildNode, out int depth)
			{
				Assert.IsNotNull(buildNode.child);
				BuildNode current = buildNode.child;

				Span<uint> children = stackalloc uint[Width];

				children.Fill(EmptyNode);

				int child = -1;
				depth = 0;

				while (current != null && ++child < Width)
				{
					uint nodeChild;
					int nodeDepth;

					if (current.child == null)
					{
						nodeChild = tokens[current.source.index];
						nodeDepth = 1;
					}
					else
					{
						uint index = (uint)++nodeIndex;
						ref Node node = ref nodes[index];

						node = CreateNode(current, out nodeDepth);
						nodeChild = index + NodeThreshold;
					}

					children[child] = nodeChild;
					depth = Math.Max(depth, nodeDepth);

					current = current.sibling;
				}

				++depth;
				return new Node(buildNode, children);
			}
		}

		readonly Node[] nodes;
		readonly int stackSize;

		/// <summary>
		/// If a child pointer in <see cref="Node"/> has this value,
		/// it means that is a null pointer and there is no child.
		/// </summary>
		const uint EmptyNode = ~0u;

		/// <summary>
		/// This is the width of the multiple processing size.  
		/// </summary>
		const int Width = 4;

		public override int Hash
		{
			get
			{
				int hash = stackSize;

				foreach (Node node in nodes) hash = (hash * 397) ^ node.GetHashCode();

				return hash;
			}
		}

		public override void GetIntersection(ref HitQuery query)
		{
			Traverse(ref query);
		}

		public override int GetIntersectionCost(in Ray ray, ref float distance) => throw new NotImplementedException();

		public override int FillAABB(int depth, Span<AxisAlignedBoundingBox> span) => throw new NotImplementedException();

		unsafe void Traverse(ref HitQuery query)
		{
			uint* stack = stackalloc uint[stackSize];
			float* hits = stackalloc float[stackSize];

			uint* next = stack;

			*next++ = 0;  //Add the index of the root node to the stack
			*hits++ = 0f; //stackalloc does not guarantee data to be zero, we have to manually assign it

			while (next != stack)
			{
				uint index = *--next;

				if (*--hits >= query.distance) continue;
				ref readonly Node node = ref nodes[index];

				Vector128<float> intersections = node.aabb4.Intersect(query.traceRay);
				Vector128<uint> children = node.child4;

				float* hit4 = (float*)&intersections;
				uint* child4 = (uint*)&children;

				int count = node.childCount;
				Sort4(hit4, child4, count);

				for (int i = 0; i < count; i++)
				{
					float hit = hit4[i];
					uint child = child4[i];

					if (child == EmptyNode) continue;
					if (float.IsNegativeInfinity(hit)) break;

					if (child < NodeThreshold)
					{
						//Child is leaf
						pack.GetIntersection(ref query, child);
					}
					else if (hit < query.distance)
					{
						//Child is branch
						*next++ = child - NodeThreshold;
						*hits++ = hit;
					}
				}
			}
		}

		static unsafe void Sort4(float* pointer0, uint* pointer1, int count)
		{
			Assert.IsTrue(count <= Width);

			for (int i = 0; i < count - 1; i++)
			{
				float max = pointer0[i];
				int index = i;

				for (int j = i + 1; j < count; j++)
				{
					float value = pointer0[j];
					if (value <= max) continue;

					max = value;
					index = j;
				}

				// if (index == i) continue;

				CodeHelper.Swap(ref pointer0[i], ref pointer0[index]);
				CodeHelper.Swap(ref pointer1[i], ref pointer1[index]);
			}
		}

		[StructLayout(LayoutKind.Sequential, Size = 128)]
		readonly struct Node
		{
			public unsafe Node(BuildNode node, ReadOnlySpan<uint> children)
			{
				Assert.IsNotNull(node.child);
				Unsafe.SkipInit(out padding);
				BuildNode child = node.child;

				ref readonly var aabb0 = ref GetAABB(ref child);
				ref readonly var aabb1 = ref GetAABB(ref child);
				ref readonly var aabb2 = ref GetAABB(ref child);
				ref readonly var aabb3 = ref GetAABB(ref child);

				aabb4 = new AxisAlignedBoundingBox4(aabb0, aabb1, aabb2, aabb3);
				fixed (uint* pointer = children) child4 = *(Vector128<uint>*)pointer;

				int count = Width - 1;

				while (children[count] == EmptyNode) --count;

				childCount = count + 1;

				static ref readonly AxisAlignedBoundingBox GetAABB(ref BuildNode node)
				{
					if (node == null) return ref AxisAlignedBoundingBox.zero;
					ref readonly AxisAlignedBoundingBox aabb = ref node.source.aabb;

					node = node.sibling;
					return ref aabb;
				}
			}

			public readonly AxisAlignedBoundingBox4 aabb4;
			public readonly Vector128<uint> child4;

			public readonly int childCount;

			// public readonly int axis0;
			// public readonly int axis1;
			// public readonly int axis2;

			readonly Int3 padding;

			public override int GetHashCode()
			{
				unchecked
				{
					int hashCode = aabb4.GetHashCode();
					hashCode = (hashCode * 397) ^ Utilities.GetHashCode(child4);
					return hashCode;
				}
			}
		}

		class BuildNode
		{
			public BuildNode(BranchBuilder.Node source, ref int count, BuildNode sibling = null)
			{
				this.source = source;
				this.sibling = sibling;
				if (source.IsLeaf) return;

				++count;

				var node0 = source.child0;
				var node1 = source.child1;

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

				static void AddChild(BranchBuilder.Node node, ref int count, ref BuildNode children) => children = new BuildNode(node, ref count, children);
			}

			public readonly BranchBuilder.Node source;

			public readonly BuildNode child;   //Linked list to the first child
			public readonly BuildNode sibling; //Reference to the next sibling node
		}
	}
}