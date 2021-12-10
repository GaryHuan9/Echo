using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using EchoRenderer.Mathematics.Intersections;

namespace EchoRenderer.Mathematics.Accelerators
{
	/// <summary>
	/// A four-way hierarchical spacial partitioning acceleration structure.
	/// Works best with very large quantities of geometries and tokens.
	/// There must be more than one token and <see cref="AxisAlignedBoundingBox"/> to process.
	/// ref: https://www.uni-ulm.de/fileadmin/website_uni_ulm/iui.inst.100/institut/Papers/QBVH.pdf
	/// </summary>
	public class QuadBoundingVolumeHierarchy : Accelerator
	{
		public QuadBoundingVolumeHierarchy(PressedPack pack, IReadOnlyList<AxisAlignedBoundingBox> aabbs, IReadOnlyList<uint> tokens) : base(pack, aabbs, tokens)
		{
			if (tokens.Count <= 1) throw ExceptionHelper.Invalid(nameof(tokens.Count), tokens.Count, "does not contain more than one token");

			int[] indices = Enumerable.Range(0, aabbs.Count).ToArray();

			BranchBuilder builder = new BranchBuilder(aabbs);
			BranchBuilder.Node root = builder.Build(indices);

			int count = 0;
			var buildRoot = new BuildNode(root, ref count);

			int nodeIndex = 0;

			nodes = new Node[count];
			nodes[0] = CreateNode(buildRoot, out int maxDepth);

			stackSize = maxDepth * 3;
			aabbRoot = root.aabb;

			Node CreateNode(BuildNode buildNode, out int depth)
			{
				Assert.IsNotNull(buildNode.child);
				BuildNode current = buildNode.child;

				Span<uint> children = stackalloc uint[Width];

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
		readonly AxisAlignedBoundingBox aabbRoot;

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

		public override void Trace(ref TraceQuery query)
		{
			Traverse(ref query);
		}

		public override int TraceCost(in Ray ray, ref float distance) => GetIntersectionCost(NodeThreshold, ray, ref distance);

		public override unsafe int FillAABB(uint depth, Span<AxisAlignedBoundingBox> span)
		{
			int length = 1 << (int)depth;
			if (length > span.Length) throw new Exception($"{nameof(span)} is not large enough! Length: '{span.Length}'");

			//Span is too small
			if (length < Width)
			{
				span[0] = aabbRoot;
				return 1;
			}

			//Because we fetch the aabbs in packs of Width (4) moving down one more level yields 4x more aabbs
			//Thus, we must carefully reduce the value of depth to make sure that we never exceed the span size
			depth = depth / 2 - 1;

			uint* stack0 = stackalloc uint[length];
			uint* stack1 = stackalloc uint[length];

			uint* next0 = stack0;
			uint* next1 = stack1;

			*next0++ = 0; //Root at 0
			int head = 0; //Result head

			for (int i = 0; i < depth; i++)
			{
				while (next0 != stack0)
				{
					ref readonly Node node = ref nodes[*--next0];

					for (int j = 0; j < Width; j++)
					{
						uint child = node.children.GetElement(j);
						if (child == EmptyNode) continue;

						if (child >= NodeThreshold) *next1++ = child - NodeThreshold;
						else span[head++] = node.aabb4.Extract(j);
					}
				}

				//Swap the two stacks
				Swap(ref next0, ref next1);
				Swap(ref stack0, ref stack1);

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void Swap(ref uint* pointer0, ref uint* pointer1)
				{
					var storage = pointer0;
					pointer0 = pointer1;
					pointer1 = storage;
				}
			}

			//Export results
			while (next0 != stack0)
			{
				ref readonly Node node = ref nodes[*--next0];

				for (int i = 0; i < Width; i++)
				{
					uint child = node.children.GetElement(i);
					if (child == EmptyNode) continue;
					span[head++] = node.aabb4.Extract(i);
				}
			}

			return head;
		}

		[SkipLocalsInit]
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		unsafe void Traverse(ref TraceQuery query)
		{
			uint* stack = stackalloc uint[stackSize];
			float* hits = stackalloc float[stackSize];

			uint* next = stack;

			*next++ = 0;  //Push the index of the root node to the stack
			*hits++ = 0f; //stackalloc does not guarantee data to be zero, we have to manually assign it

			bool* orders = stackalloc bool[Width]
			{
				query.ray.inverseDirection.x > 0,
				query.ray.inverseDirection.y > 0,
				query.ray.inverseDirection.z > 0,
				true
			};

			while (next != stack)
			{
				uint index = *--next;

				if (*--hits >= query.distance) continue;
				ref readonly Node node = ref nodes[index];

				Vector128<float> intersections = node.aabb4.Intersect(query.ray);
				float* i = (float*)&intersections;

				if (orders[node.axisMajor])
				{
					if (orders[node.axisMinor1])
					{
						Push(i[3], node.child3, ref query);
						Push(i[2], node.child2, ref query);
					}
					else
					{
						Push(i[2], node.child2, ref query);
						Push(i[3], node.child3, ref query);
					}

					if (orders[node.axisMinor0])
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
					if (orders[node.axisMinor0])
					{
						Push(i[1], node.child1, ref query);
						Push(i[0], node.child0, ref query);
					}
					else
					{
						Push(i[0], node.child0, ref query);
						Push(i[1], node.child1, ref query);
					}

					if (orders[node.axisMinor1])
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
				void Push(float hit, uint child, ref TraceQuery hitQuery)
				{
					if (hit >= hitQuery.distance) return;

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

		int GetIntersectionCost(uint index, in Ray ray, ref float distance, float intersection = 0f)
		{
			if (index == EmptyNode || intersection >= distance) return 0;
			if (index < NodeThreshold) return pack.GetIntersectionCost(ray, ref distance, index);

			ref readonly Node node = ref nodes[index - NodeThreshold];
			Vector128<float> intersections = node.aabb4.Intersect(ray);

			Span<bool> orders = stackalloc bool[Width]
			{
				ray.inverseDirection.x > 0f,
				ray.inverseDirection.y > 0f,
				ray.inverseDirection.z > 0f,
				true
			};

			int cost = 4;

			if (orders[node.axisMajor])
			{
				cost += GetIntersectionCost2(orders[node.axisMinor0], intersections.GetLower(), node.children.GetLower(), ray, ref distance);
				cost += GetIntersectionCost2(orders[node.axisMinor1], intersections.GetUpper(), node.children.GetUpper(), ray, ref distance);
			}
			else
			{
				cost += GetIntersectionCost2(orders[node.axisMinor1], intersections.GetUpper(), node.children.GetUpper(), ray, ref distance);
				cost += GetIntersectionCost2(orders[node.axisMinor0], intersections.GetLower(), node.children.GetLower(), ray, ref distance);
			}

			return cost;
		}

		int GetIntersectionCost2(bool order, in Vector64<float> intersections, in Vector64<uint> children, in Ray ray, ref float distance)
		{
			int cost = 0;

			if (order)
			{
				cost += GetIntersectionCost(children.GetElement(0), ray, ref distance, intersections.GetElement(0));
				cost += GetIntersectionCost(children.GetElement(1), ray, ref distance, intersections.GetElement(1));
			}
			else
			{
				cost += GetIntersectionCost(children.GetElement(1), ray, ref distance, intersections.GetElement(1));
				cost += GetIntersectionCost(children.GetElement(0), ray, ref distance, intersections.GetElement(0));
			}

			return cost;
		}

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

				Unsafe.SkipInit(out padding);
				Unsafe.SkipInit(out this.children);

				static ref readonly AxisAlignedBoundingBox GetAABB(ref BuildNode node)
				{
					var source = node.source;
					node = node.sibling;

					if (source != null) return ref source.aabb;
					return ref AxisAlignedBoundingBox.none;
				}
			}

			[FieldOffset(0)] public readonly AxisAlignedBoundingBox4 aabb4;
			[FieldOffset(096)] public readonly Vector128<uint> children;

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