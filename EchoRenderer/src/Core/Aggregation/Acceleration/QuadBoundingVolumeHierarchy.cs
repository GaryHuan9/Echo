﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Core.Aggregation.Preparation;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Scenic.Preparation;

namespace EchoRenderer.Core.Aggregation.Acceleration;

/// <summary>
/// A four-way hierarchical spacial partitioning acceleration structure.
/// Works best with very large quantities of geometries and tokens.
/// There must be more than one token and <see cref="AxisAlignedBoundingBox"/> to process.
/// ref: https://www.uni-ulm.de/fileadmin/website_uni_ulm/iui.inst.100/institut/Papers/QBVH.pdf
/// </summary>
public class QuadBoundingVolumeHierarchy : Aggregator
{
	public QuadBoundingVolumeHierarchy(PreparedPack pack, ReadOnlyMemory<AxisAlignedBoundingBox> aabbs, ReadOnlySpan<NodeToken> tokens) : base(pack)
	{
		Validate(aabbs, tokens, length => length > 1);
		int[] indices = CreateIndices(aabbs.Length);

		BranchBuilder builder = new BranchBuilder(aabbs);
		BranchBuilder.Node root = builder.Build(indices);

		int count = 0;
		var buildRoot = new BuildNode(root, ref count);

		uint nodeIndex = 1;

		nodes = new Node[count];
		nodes[0] = CreateNode(buildRoot, tokens, ref nodeIndex, out int maxDepth);

		stackSize = maxDepth * 3;
		rootAABB = root.aabb;

		Assert.AreEqual((long)nodeIndex, nodes.Length);
	}

	readonly Node[] nodes;
	readonly int stackSize;
	readonly AxisAlignedBoundingBox rootAABB;

	/// <summary>
	/// This is the width of the multiple processing size.  
	/// </summary>
	const int Width = 4;

	public override void Trace(ref TraceQuery query) => TraceCore(ref query);

	public override bool Occlude(ref OccludeQuery query) => OccludeCore(ref query);

	public override int TraceCost(in Ray ray, ref float distance) => GetTraceCost(NodeToken.root, ray, ref distance);

	public override unsafe int GetHashCode()
	{
		fixed (Node* ptr = nodes) return Utilities.GetHashCode(ptr, (uint)nodes.Length, stackSize);
	}

	public override unsafe int FillAABB(uint depth, Span<AxisAlignedBoundingBox> span)
	{
		int length = 1 << (int)depth;
		if (length > span.Length) throw new Exception($"{nameof(span)} is not large enough! Length: '{span.Length}'");

		//Span is too small
		if (length < Width)
		{
			span[0] = rootAABB;
			return 1;
		}

		//Because we fetch the aabbs in packs of Width (4) moving down one more level yields 4x more aabbs
		//Thus, we must carefully reduce the value of depth to make sure that we never exceed the span size
		depth = depth / 2 - 1;

		NodeToken* stack0 = stackalloc NodeToken[length];
		NodeToken* stack1 = stackalloc NodeToken[length];

		NodeToken* next0 = stack0;
		NodeToken* next1 = stack1;

		*next0++ = NodeToken.root;
		int head = 0; //Result head

		for (int i = 0; i < depth; i++)
		{
			while (next0 != stack0)
			{
				ref readonly Node node = ref nodes[(--next0)->NodeValue];

				for (int j = 0; j < Width; j++)
				{
					ref readonly NodeToken child = ref node.token4[j];

					if (child.IsEmpty) continue;
					if (child.IsNode) *next1++ = child;
					else span[head++] = node.aabb4[j];
				}
			}

			//Swap the two stacks
			Swap(ref next0, ref next1);
			Swap(ref stack0, ref stack1);
		}

		//Export results
		while (next0 != stack0)
		{
			ref readonly Node node = ref nodes[(--next0)->NodeValue];

			for (int i = 0; i < Width; i++)
			{
				ref readonly NodeToken child = ref node.token4[i];
				if (!child.IsEmpty) span[head++] = node.aabb4[i];
			}
		}

		return head;
	}

	[SkipLocalsInit]
	[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
	unsafe void TraceCore(ref TraceQuery query)
	{
		NodeToken* stack = stackalloc NodeToken[stackSize];
		float* hits = stackalloc float[stackSize];

		NodeToken* next = stack;

		*next++ = NodeToken.root;
		*hits++ = 0f;

		bool* orders = stackalloc bool[Width]
		{
			query.ray.inverseDirection.x > 0,
			query.ray.inverseDirection.y > 0,
			query.ray.inverseDirection.z > 0,
			true
		};

		while (next != stack)
		{
			uint index = (--next)->NodeValue;

			if (*--hits >= query.distance) continue;
			ref readonly Node node = ref nodes[index];

			Vector128<float> intersections = node.aabb4.Intersect(query.ray);

			if (orders[node.axisMajor])
			{
				if (orders[node.axisMinor1])
				{
					Push(intersections, node.token4, 3, ref query);
					Push(intersections, node.token4, 2, ref query);
				}
				else
				{
					Push(intersections, node.token4, 2, ref query);
					Push(intersections, node.token4, 3, ref query);
				}

				if (orders[node.axisMinor0])
				{
					Push(intersections, node.token4, 1, ref query);
					Push(intersections, node.token4, 0, ref query);
				}
				else
				{
					Push(intersections, node.token4, 0, ref query);
					Push(intersections, node.token4, 1, ref query);
				}
			}
			else
			{
				if (orders[node.axisMinor0])
				{
					Push(intersections, node.token4, 1, ref query);
					Push(intersections, node.token4, 0, ref query);
				}
				else
				{
					Push(intersections, node.token4, 0, ref query);
					Push(intersections, node.token4, 1, ref query);
				}

				if (orders[node.axisMinor1])
				{
					Push(intersections, node.token4, 3, ref query);
					Push(intersections, node.token4, 2, ref query);
				}
				else
				{
					Push(intersections, node.token4, 2, ref query);
					Push(intersections, node.token4, 3, ref query);
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
			void Push(in Vector128<float> hit4, in NodeToken4 child4, int offset, ref TraceQuery refQuery)
			{
				float hit = hit4.GetElement(offset);
				if (hit >= refQuery.distance) return;
				ref readonly NodeToken token = ref child4[offset];

				if (token.IsGeometry)
				{
					//Child is leaf
					pack.Trace(ref refQuery, token);
				}
				else
				{
					//Child is branch
					*next++ = token;
					*hits++ = hit;
				}
			}
		}
	}

	[SkipLocalsInit]
	[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
	unsafe bool OccludeCore(ref OccludeQuery query)
	{
		NodeToken* stack = stackalloc NodeToken[stackSize];

		NodeToken* next = stack;
		*next++ = NodeToken.root;

		bool* orders = stackalloc bool[Width]
		{
			query.ray.inverseDirection.x > 0,
			query.ray.inverseDirection.y > 0,
			query.ray.inverseDirection.z > 0,
			true
		};

		while (next != stack)
		{
			ref readonly Node node = ref nodes[(--next)->NodeValue];
			Vector128<float> intersections = node.aabb4.Intersect(query.ray);

			if (orders[node.axisMajor])
			{
				if (orders[node.axisMinor1])
				{
					if (Push(intersections, node.token4, 3, ref query)) return true;
					if (Push(intersections, node.token4, 2, ref query)) return true;
				}
				else
				{
					if (Push(intersections, node.token4, 2, ref query)) return true;
					if (Push(intersections, node.token4, 3, ref query)) return true;
				}

				if (orders[node.axisMinor0])
				{
					if (Push(intersections, node.token4, 1, ref query)) return true;
					if (Push(intersections, node.token4, 0, ref query)) return true;
				}
				else
				{
					if (Push(intersections, node.token4, 0, ref query)) return true;
					if (Push(intersections, node.token4, 1, ref query)) return true;
				}
			}
			else
			{
				if (orders[node.axisMinor0])
				{
					if (Push(intersections, node.token4, 1, ref query)) return true;
					if (Push(intersections, node.token4, 0, ref query)) return true;
				}
				else
				{
					if (Push(intersections, node.token4, 0, ref query)) return true;
					if (Push(intersections, node.token4, 1, ref query)) return true;
				}

				if (orders[node.axisMinor1])
				{
					if (Push(intersections, node.token4, 3, ref query)) return true;
					if (Push(intersections, node.token4, 2, ref query)) return true;
				}
				else
				{
					if (Push(intersections, node.token4, 2, ref query)) return true;
					if (Push(intersections, node.token4, 3, ref query)) return true;
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
			bool Push(in Vector128<float> hit4, in NodeToken4 child4, int offset, ref OccludeQuery refQuery)
			{
				float hit = hit4.GetElement(offset);
				if (hit >= refQuery.travel) return false;
				ref readonly NodeToken token = ref child4[offset];

				if (token.IsGeometry) return pack.Occlude(ref refQuery, token); //Child is leaf

				//Child is branch
				*next++ = token;
				return false;
			}
		}

		return false;
	}

	int GetTraceCost(in NodeToken token, in Ray ray, ref float distance, float intersection = float.NegativeInfinity)
	{
		if (token.IsEmpty || intersection >= distance) return 0;
		if (token.IsGeometry) return pack.GetTraceCost(ray, ref distance, token);

		ref readonly Node node = ref nodes[token.NodeValue];
		Vector128<float> intersections = node.aabb4.Intersect(ray);

		Span<bool> orders = stackalloc bool[Width]
		{
			ray.inverseDirection.x > 0f,
			ray.inverseDirection.y > 0f,
			ray.inverseDirection.z > 0f,
			true
		};

		int cost = Width;

		if (orders[node.axisMajor])
		{
			cost += GetTraceCost2(orders[node.axisMinor0], 0, node.token4, intersections, ray, ref distance);
			cost += GetTraceCost2(orders[node.axisMinor1], 2, node.token4, intersections, ray, ref distance);
		}
		else
		{
			cost += GetTraceCost2(orders[node.axisMinor1], 2, node.token4, intersections, ray, ref distance);
			cost += GetTraceCost2(orders[node.axisMinor0], 0, node.token4, intersections, ray, ref distance);
		}

		return cost;
	}

	int GetTraceCost2(bool order, int offset, in NodeToken4 child4, in Vector128<float> intersections, in Ray ray, ref float distance)
	{
		int cost = 0;

		if (order)
		{
			cost += GetTraceCost(child4[0 + offset], ray, ref distance, intersections.GetElement(0 + offset));
			cost += GetTraceCost(child4[1 + offset], ray, ref distance, intersections.GetElement(1 + offset));
		}
		else
		{
			cost += GetTraceCost(child4[1 + offset], ray, ref distance, intersections.GetElement(1 + offset));
			cost += GetTraceCost(child4[0 + offset], ray, ref distance, intersections.GetElement(0 + offset));
		}

		return cost;
	}

	Node CreateNode(BuildNode buildNode, ReadOnlySpan<NodeToken> tokens, ref uint nodeIndex, out int depth)
	{
		Assert.IsNotNull(buildNode.child);
		BuildNode current = buildNode.child;

		Span<NodeToken> token4 = stackalloc NodeToken[Width];

		depth = 0;

		for (int i = 0; i < Width; i++)
		{
			NodeToken nodeToken;
			int nodeDepth;

			if (current.IsEmpty)
			{
				nodeToken = NodeToken.empty;
				nodeDepth = 0;
			}
			else if (current.IsLeaf)
			{
				nodeToken = tokens[current.source.index];
				nodeDepth = 1;
			}
			else
			{
				uint index = nodeIndex++;
				ref Node node = ref nodes[index];

				node = CreateNode(current, tokens, ref nodeIndex, out nodeDepth);
				nodeToken = NodeToken.CreateNode(index);
			}

			token4[i] = nodeToken;
			depth = Math.Max(depth, nodeDepth);

			current = current.sibling;
		}

		++depth;
		return new Node(buildNode, token4);
	}

	/// <summary>
	/// The node is only 124-byte in size, however we pad it to 128 bytes to better align with cache lines and memory stuff.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 128)]
	readonly struct Node
	{
		public Node(BuildNode node, ReadOnlySpan<NodeToken> token4)
		{
			Assert.IsNotNull(node.child);
			BuildNode child = node.child;

			ref readonly var aabb0 = ref GetAABB(ref child);
			ref readonly var aabb1 = ref GetAABB(ref child);
			ref readonly var aabb2 = ref GetAABB(ref child);
			ref readonly var aabb3 = ref GetAABB(ref child);

			aabb4 = new AxisAlignedBoundingBox4(aabb0, aabb1, aabb2, aabb3);
			this.token4 = new NodeToken4(token4);

			axisMajor = node.axisMajor;
			axisMinor0 = node.axisMinor0;
			axisMinor1 = node.axisMinor1;

			padding = default;

			static ref readonly AxisAlignedBoundingBox GetAABB(ref BuildNode node)
			{
				var source = node.source;
				node = node.sibling;

				if (source != null) return ref source.aabb;
				return ref AxisAlignedBoundingBox.none;
			}
		}

		/// <summary>
		/// Static check to ensure our currently layout is correctly updated if <see cref="NodeToken4"/> size is changed.
		/// </summary>
		static unsafe Node()
		{
			int size = sizeof(NodeToken4);

			if (size is Width * NodeToken.Size and 16) return;
			throw new Exception("Invalid layout or size!");
		}

		/// <summary>
		/// The <see cref="AxisAlignedBoundingBox"/>s of the four children contained in this <see cref="Node"/>.
		/// </summary>
		[FieldOffset(0)] public readonly AxisAlignedBoundingBox4 aabb4;

		/// <summary>
		/// The <see cref="NodeToken"/> representing the four branches off from this <see cref="Node"/>,
		/// which is either a leaf geometry object or another <see cref="Node"/> child branch.
		/// </summary>
		[FieldOffset(096)] public readonly NodeToken4 token4;

		/// <summary>
		/// Unused memory padding.
		/// </summary>
		[FieldOffset(112)] readonly int padding;

		/// <summary>
		/// The integer value of the axis that divided the two primary nodes.
		/// </summary>
		[FieldOffset(116)] public readonly int axisMajor;

		/// <summary>
		/// The integer value of the axis that divided the first two secondary nodes.
		/// </summary>
		[FieldOffset(120)] public readonly int axisMinor0;

		/// <summary>
		/// The integer value of the axis that divided the second two secondary nodes.
		/// </summary>
		[FieldOffset(124)] public readonly int axisMinor1;
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