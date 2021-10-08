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

			int count = 0;
			var buildRoot = new BuildNode(root, ref count);

			int nodeIndex = 0;

			nodes = new Node[count];
			nodes[0] = CreateNode(buildRoot, out int maxDepth);

			stackSize = maxDepth * 3;

			Node CreateNode(BuildNode buildNode, out int depth)
			{
				Assert.IsNotNull(buildNode.child);
				BuildNode current = buildNode.child;

				Span<uint> children = stackalloc uint[Width];

				children.Fill(EmptyNode);

				depth = 0;

				for (int i = 0; i < Width; i++)
				{
					uint nodeChild;
					int nodeDepth;

					if (current.IsEmpty)
					{
						nodeChild = EmptyNode;
						nodeDepth = 0;
					}
					else if (current.IsLeaf)
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

					children[i] = nodeChild;
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
			Traverse0(ref query);
		}

		public override int GetIntersectionCost(in Ray ray, ref float distance) => throw new NotImplementedException();

		public override int FillAABB(int depth, Span<AxisAlignedBoundingBox> span) => throw new NotImplementedException();

		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		unsafe void Traverse0(ref HitQuery query)
		{
			uint* stack = stackalloc uint[stackSize];
			float* hits = stackalloc float[stackSize];

			uint* next = stack;

			*next++ = 0;  //Push the index of the root node to the stack
			*hits++ = 0f; //stackalloc does not guarantee data to be zero, we have to manually assign it

			float* direction = stackalloc float[Width];
			*(Float3*)direction = query.ray.inverseDirection;
			direction[Width - 1] = 1f;

			while (next != stack)
			{
				uint index = *--next;

				if (*--hits >= query.distance) continue;
				ref readonly Node node = ref nodes[index];

				Vector128<float> intersections = node.aabb4.Intersect(query.ray);
				float* i = (float*)&intersections;

				if (direction[node.axisMajor] > 0)
				{
					if (direction[node.axisMinor1] > 0)
					{
						Push(i[3], node.child3, ref query);
						Push(i[2], node.child2, ref query);
					}
					else
					{
						Push(i[2], node.child2, ref query);
						Push(i[3], node.child3, ref query);
					}

					if (direction[node.axisMinor0] > 0)
					{
						Push(i[1], node.child1, ref query);
						Push(i[0], node.child0, ref query);
					}
					else
					{
						Push(i[0], node.child0, ref query);
						Push(i[1], node.child1, ref query);
					}
				}
				else
				{
					if (direction[node.axisMinor0] > 0)
					{
						Push(i[1], node.child1, ref query);
						Push(i[0], node.child0, ref query);
					}
					else
					{
						Push(i[0], node.child0, ref query);
						Push(i[1], node.child1, ref query);
					}

					if (direction[node.axisMinor1] > 0)
					{
						Push(i[3], node.child3, ref query);
						Push(i[2], node.child2, ref query);
					}
					else
					{
						Push(i[2], node.child2, ref query);
						Push(i[3], node.child3, ref query);
					}
				}

				[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
				void Push(float hit, uint child, ref HitQuery hitQuery)
				{
					if (child == EmptyNode || hit >= hitQuery.distance) return;

					if (child < NodeThreshold)
					{
						//Child is leaf
						pack.GetIntersection(ref hitQuery, child);
					}
					else
					{
						//Child is branch
						*next++ = child - NodeThreshold;
						*hits++ = hit;
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		unsafe void Traverse1(ref HitQuery query)
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

				Vector128<float> intersections = node.aabb4.Intersect(query.ray);
				Vector128<uint> children = node.children;

				float* hit4 = (float*)&intersections;
				uint* child4 = (uint*)&children;

				Sort4(hit4, child4);

				for (int i = Width - 1; i >= 0; i--)
				{
					float hit = hits[i];
					uint child = child4[i];

					if (hit >= query.distance || child == EmptyNode) continue;

					if (child < NodeThreshold)
					{
						//Child is leaf
						pack.GetIntersection(ref query, child);
					}
					else
					{
						//Child is branch
						*next++ = child - NodeThreshold;
						*hits++ = hit;
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		static unsafe void Sort4(float* keys, uint* values)
		{
			ConditionalSwap(0, 2);
			ConditionalSwap(1, 3);
			ConditionalSwap(0, 1);
			ConditionalSwap(2, 3);
			ConditionalSwap(1, 2);

			[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
			void ConditionalSwap(int index0, int index1)
			{
				if (keys[index0] <= keys[index1]) return;

				CodeHelper.Swap(ref keys[index0], ref keys[index1]);
				CodeHelper.Swap(ref values[index0], ref values[index1]);
			}
		}

		// static void ConstructLookup()
		// {
		// 	uint[] childLookup = new uint[1 << (2 * 3 + 3)];
		//
		// 	Span<byte> span = stackalloc byte[4];
		// 	Span<int> directions = stackalloc int[4];
		// 	Span<int> axes = stackalloc int[3];
		//
		// 	for (byte axis = 0b00_0000; axis <= 0b11_1111; axis++)
		// 	{
		// 		for (byte direction = 0b000; direction <= 0b111; direction++)
		// 		{
		// 			directions[0] = (direction >> 0) & 1;
		// 			directions[1] = (direction >> 1) & 1;
		// 			directions[2] = (direction >> 2) & 1;
		// 			directions[3] = 1;
		//
		// 			axes[0] = (axis >> 0) & 0b11;
		// 			axes[1] = (axis >> 1) & 0b11;
		// 			axes[2] = (axis >> 2) & 0b11;
		//
		// 			FillSequence(span, directions, axes);
		// 			DebugHelper.Log(axis.ToStringBinary(), direction.ToStringBinary());
		// 			DebugHelper.Log(span[0], span[1], span[2], span[3]);
		// 		}
		// 	}
		//
		// 	static void FillSequence(Span<byte> span, ReadOnlySpan<int> directions, ReadOnlySpan<int> axes)
		// 	{
		// 		int head = 0;
		//
		// 		if (directions[axes[0]] > 0)
		// 		{
		// 			if (directions[axes[2]] > 0)
		// 			{
		// 				span[head++] = 3;
		// 				span[head++] = 2;
		// 			}
		// 			else
		// 			{
		// 				span[head++] = 2;
		// 				span[head++] = 3;
		// 			}
		//
		// 			if (directions[axes[1]] > 0)
		// 			{
		// 				span[head++] = 1;
		// 				span[head++] = 0;
		// 			}
		// 			else
		// 			{
		// 				span[head++] = 0;
		// 				span[head++] = 1;
		// 			}
		// 		}
		// 		else
		// 		{
		// 			if (directions[axes[1]] > 0)
		// 			{
		// 				span[head++] = 1;
		// 				span[head++] = 0;
		// 			}
		// 			else
		// 			{
		// 				span[head++] = 0;
		// 				span[head++] = 1;
		// 			}
		//
		// 			if (directions[axes[2]] > 0)
		// 			{
		// 				span[head++] = 3;
		// 				span[head++] = 2;
		// 			}
		// 			else
		// 			{
		// 				span[head++] = 2;
		// 				span[head++] = 3;
		// 			}
		// 		}
		//
		// 		Assert.AreEqual(head, span.Length);
		// 	}
		// }

		/// <summary>
		/// The node is only 124-byte in size, however we pad it to 128 bytes to better align with cache lines and memory stuff.
		/// </summary>
		[StructLayout(LayoutKind.Explicit, Size = 128)]
		readonly struct Node
		{
			public Node(BuildNode node, ReadOnlySpan<uint> children)
			{
				Assert.IsNotNull(node.child);
				BuildNode child = node.child;

				Unsafe.SkipInit(out this.children);
				Unsafe.SkipInit(out padding);

				ref readonly var aabb0 = ref GetAABB(ref child);
				ref readonly var aabb1 = ref GetAABB(ref child);
				ref readonly var aabb2 = ref GetAABB(ref child);
				ref readonly var aabb3 = ref GetAABB(ref child);

				child0 = children[0];
				child1 = children[1];
				child2 = children[2];
				child3 = children[3];

				aabb4 = new AxisAlignedBoundingBox4(aabb0, aabb1, aabb2, aabb3);

				axisMajor = node.axisMajor;
				axisMinor0 = node.axisMinor0;
				axisMinor1 = node.axisMinor1;

				static ref readonly AxisAlignedBoundingBox GetAABB(ref BuildNode node)
				{
					var source = node.source;
					node = node.sibling;

					if (source != null) return ref source.aabb;
					return ref AxisAlignedBoundingBox.zero;
				}
			}

			[FieldOffset(0)] public readonly AxisAlignedBoundingBox4 aabb4;
			[FieldOffset(96)] public readonly Vector128<uint> children;

			[FieldOffset(096)] public readonly uint child0;
			[FieldOffset(100)] public readonly uint child1;
			[FieldOffset(104)] public readonly uint child2;
			[FieldOffset(108)] public readonly uint child3;

			/// <summary>
			/// The integer value of the axis that divided the two primary nodes
			/// </summary>
			[FieldOffset(112)] public readonly int axisMajor;

			/// <summary>
			/// The integer value of the axis that divided the first two secondary nodes
			/// </summary>
			[FieldOffset(116)] public readonly int axisMinor0;

			/// <summary>
			/// The integer value of the axis that divided the second two secondary nodes
			/// </summary>
			[FieldOffset(120)] public readonly int axisMinor1;

			[FieldOffset(124)] readonly int padding;

			public override int GetHashCode()
			{
				unchecked
				{
					int hashCode = aabb4.GetHashCode();
					hashCode = (hashCode * 397) ^ (int)child0;
					hashCode = (hashCode * 397) ^ (int)child1;
					hashCode = (hashCode * 397) ^ (int)child2;
					hashCode = (hashCode * 397) ^ (int)child3;
					hashCode = (hashCode * 397) ^ axisMajor;
					hashCode = (hashCode * 397) ^ axisMinor0;
					hashCode = (hashCode * 397) ^ axisMinor1;
					return hashCode;
				}
			}
		}

		class BuildNode
		{
			/// <summary>
			/// Collapses <see cref="source"/> from a binary tree to a quad tree.
			/// This node will be the root node of that newly created quad tree.
			/// </summary>
			public BuildNode(BranchBuilder.Node source, ref int count) : this(source, null, ref count) { }

			/// <summary>
			/// Creates a new <see cref="BuildNode"/> from <paramref name="source"/>.
			/// NOTE: <paramref name="source"/> can be null to indicate an empty node,
			/// or turn <paramref name="source.IsLeaf"/> on to indicate a leaf node.
			/// </summary>
			BuildNode(BranchBuilder.Node source, BuildNode sibling, ref int count)
			{
				this.source = source;
				this.sibling = sibling;

				if (source?.IsLeaf != false) return;

				++count;

				axisMajor = GetChildrenSorted(source, out var child0, out var child1);

				//Add children as a linked list
				//Note that the order is reversed

				axisMinor1 = AddChildren(child1, ref child, ref count);
				axisMinor0 = AddChildren(child0, ref child, ref count);
			}

			public bool IsEmpty => source == null;
			public bool IsLeaf => child == null;

			public readonly BranchBuilder.Node source;

			public readonly BuildNode child;   //Linked list to the first child (4)
			public readonly BuildNode sibling; //Reference to the next sibling node

			/// <inheritdoc cref="Node.axisMajor"/>
			public readonly int axisMajor;

			/// <inheritdoc cref="Node.axisMinor0"/>
			public readonly int axisMinor0;

			/// <inheritdoc cref="Node.axisMinor1"/>
			public readonly int axisMinor1;

			/// <summary>
			/// Adds children to the linked list represented by <paramref name="child"/>. If <paramref name="node"/> is a leaf,
			/// add <paramref name="node"/> and an empty node to the linked list. Otherwise, add the two children of <paramref name="node"/>
			/// to the linked list orderly, based on the position of their aabbs along <paramref name="node.axis"/>. The node with the
			/// smaller position will be placed before the node with the larger position. Returns <paramref name="node.axis"/>.
			/// </summary>
			static int AddChildren(BranchBuilder.Node node, ref BuildNode child, ref int count)
			{
				int axis;

				if (node.IsLeaf)
				{
					//NOTE assigning axis to 3 means when we are indexing the direction, we will always get 1,
					//which means the cascading branches will always position the leaf node before the empty node.
					axis = 3;

					AddChild(null, ref child, ref count);
					AddChild(node, ref child, ref count);
				}
				else
				{
					axis = GetChildrenSorted(node, out var child0, out var child1);

					AddChild(child1, ref child, ref count);
					AddChild(child0, ref child, ref count);
				}

				return axis;
			}

			/// <summary>
			/// Outputs the two children of <paramref name="node"/> in sorted order. The children are stored based on the position of their
			/// aabbs along <paramref name="node.axis"/>. The node with the smaller position will be placed in <paramref name="child0"/> and
			/// the other one will be placed in <paramref name="child1"/>. Returns <paramref name="node.axis"/>.
			/// </summary>
			static int GetChildrenSorted(BranchBuilder.Node node, out BranchBuilder.Node child0, out BranchBuilder.Node child1)
			{
				int axis = node.axis;
				child0 = node.child0;
				child1 = node.child1;

				//Put child1 as the first child if child0 has a larger position
				if (child0.aabb.min[axis] > child1.aabb.min[axis]) CodeHelper.Swap(ref child0, ref child1);

				return axis;
			}

			/// <summary>
			/// Adds a new <see cref="BuildNode"/> to the linked list represented by <paramref name="child"/>.
			/// </summary>
			static void AddChild(BranchBuilder.Node node, ref BuildNode child, ref int count) => child = new BuildNode(node, child, ref count);
		}
	}
}