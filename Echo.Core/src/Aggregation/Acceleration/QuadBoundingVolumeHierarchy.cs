using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common;
using Echo.Core.Common.Memory;

namespace Echo.Core.Aggregation.Acceleration;

/// <summary>
/// A four-way hierarchical spacial partitioning acceleration structure.
/// Works best with very large quantities of geometries and tokens.
/// There must be more than one token and <see cref="AxisAlignedBoundingBox"/> to process.
/// ref: https://www.uni-ulm.de/fileadmin/website_uni_ulm/iui.inst.100/institut/Papers/QBVH.pdf
/// </summary>
public class QuadBoundingVolumeHierarchy : Accelerator
{
	public QuadBoundingVolumeHierarchy(GeometryCollection geometries, ReadOnlyView<AxisAlignedBoundingBox> aabbs, ReadOnlySpan<EntityToken> tokens) : base(geometries)
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

		stackSize = maxDepth * 3 + 1; //NOTE: this equation is not arbitrary!
		Assert.AreEqual((long)nodeIndex, nodes.Length);
	}

	readonly Node[] nodes;
	readonly int stackSize;

	/// <summary>
	/// This is the width of the multiple processing size.  
	/// </summary>
	const int Width = 4;

	/// <summary>
	/// Returns the root <see cref="AxisAlignedBoundingBox"/> that encapsulates the entire hierarchy.
	/// </summary>
	AxisAlignedBoundingBox RootAABB => nodes[RootToken.Index].aabb4.Encapsulated;

	static EntityToken RootToken => NewNodeToken(0);

	public override void Trace(ref TraceQuery query) => TraceImpl(ref query);
	public override bool Occlude(ref OccludeQuery query) => OccludeImpl(ref query);
	public override uint TraceCost(in Ray ray, ref float distance) => GetTraceCost(RootToken, ray, ref distance);

	public override unsafe int GetHashCode()
	{
		fixed (Node* ptr = nodes) return Utility.GetHashCode(ptr, (uint)nodes.Length, stackSize);
	}

	public override unsafe void FillBounds(uint depth, ref SpanFill<AxisAlignedBoundingBox> fill)
	{
		int length = 1 << (int)depth;
		fill.ThrowIfTooSmall(length);
		fill.ThrowIfNotEmpty();

		//Output is too small
		if (length < Width)
		{
			fill.Add(RootAABB);
			return;
		}

		//Because we fetch the aabbs in packs of Width (4), moving down one more level yields 4x more aabbs
		//Thus, we must carefully reduce the value of depth to make sure that we never exceed the span size
		uint iteration = depth / 2 - 1;

		EntityToken* stack0 = stackalloc EntityToken[length];
		EntityToken* stack1 = stackalloc EntityToken[length];

		EntityToken* next0 = stack0;
		EntityToken* next1 = stack1;

		*next0++ = RootToken;

		for (int i = 0; i < iteration; i++)
		{
			while (next0 != stack0)
			{
				ref readonly Node node = ref nodes[(--next0)->Index];

				for (int j = 0; j < Width; j++)
				{
					ref readonly EntityToken child = ref node.token4[j];

					if (child.IsEmpty) continue;
					if (child.Type == TokenType.Node) *next1++ = child;
					else fill.Add(node.aabb4[j]);
				}
			}

			//Swap the two stacks
			Swap(ref next0, ref next1);
			Swap(ref stack0, ref stack1);
		}

		//Export results
		while (next0 != stack0)
		{
			ref readonly Node node = ref nodes[(--next0)->Index];

			for (int i = 0; i < Width; i++)
			{
				ref readonly EntityToken child = ref node.token4[i];
				if (!child.IsEmpty) fill.Add(node.aabb4[i]);
			}
		}
	}

	[SkipLocalsInit]
	[MethodImpl(ImplementationOptions)]
	unsafe void TraceImpl(ref TraceQuery query)
	{
		var stack = stackalloc EntityToken[stackSize];
		float* hits = stackalloc float[stackSize];

		EntityToken* next = stack;
		*next++ = NewNodeToken(0);
		*hits++ = 0f;

		bool* orders = stackalloc bool[Width]
		{
			query.ray.directionR.X > 0,
			query.ray.directionR.Y > 0,
			query.ray.directionR.Z > 0,
			true
		};

		ref Node origin = ref nodes[0];

		do
		{
			uint index = (--next)->Index;

			if (*--hits >= query.distance) continue;

			ref readonly Node node = ref Unsafe.Add(ref origin, index);
			Float4 intersections = node.aabb4.Intersect(query.ray);

			if (orders[node.axisMajor])
			{
				if (orders[node.axisMinor1])
				{
					Push(intersections, node.token4, geometries, 3, ref next, ref hits, ref query);
					Push(intersections, node.token4, geometries, 2, ref next, ref hits, ref query);
				}
				else
				{
					Push(intersections, node.token4, geometries, 2, ref next, ref hits, ref query);
					Push(intersections, node.token4, geometries, 3, ref next, ref hits, ref query);
				}

				if (orders[node.axisMinor0])
				{
					Push(intersections, node.token4, geometries, 1, ref next, ref hits, ref query);
					Push(intersections, node.token4, geometries, 0, ref next, ref hits, ref query);
				}
				else
				{
					Push(intersections, node.token4, geometries, 0, ref next, ref hits, ref query);
					Push(intersections, node.token4, geometries, 1, ref next, ref hits, ref query);
				}
			}
			else
			{
				if (orders[node.axisMinor0])
				{
					Push(intersections, node.token4, geometries, 1, ref next, ref hits, ref query);
					Push(intersections, node.token4, geometries, 0, ref next, ref hits, ref query);
				}
				else
				{
					Push(intersections, node.token4, geometries, 0, ref next, ref hits, ref query);
					Push(intersections, node.token4, geometries, 1, ref next, ref hits, ref query);
				}

				if (orders[node.axisMinor1])
				{
					Push(intersections, node.token4, geometries, 3, ref next, ref hits, ref query);
					Push(intersections, node.token4, geometries, 2, ref next, ref hits, ref query);
				}
				else
				{
					Push(intersections, node.token4, geometries, 2, ref next, ref hits, ref query);
					Push(intersections, node.token4, geometries, 3, ref next, ref hits, ref query);
				}
			}

			[MethodImpl(ImplementationOptions)]
			static void Push(in Float4 intersections, in EntityToken4 token4, GeometryCollection geometries,
							 int offset, ref EntityToken* next, ref float* hits, ref TraceQuery query)
			{
				float hit = intersections[offset];
				if (hit >= query.distance) return;

				ref readonly EntityToken token = ref token4[offset];

				if (!token.Type.IsGeometry())
				{
					//Child is node/branch
					*next++ = token;
					*hits++ = hit;
				}
				else geometries.Trace(ref query, token); //Child is geometry/leaf
			}
		}
		while (next != stack);
	}

	[SkipLocalsInit]
	[MethodImpl(ImplementationOptions)]
	unsafe bool OccludeImpl(ref OccludeQuery query)
	{
		EntityToken* stack = stackalloc EntityToken[stackSize];

		EntityToken* next = stack;
		*next++ = NewNodeToken(0);

		bool* orders = stackalloc bool[Width]
		{
			query.ray.directionR.X > 0,
			query.ray.directionR.Y > 0,
			query.ray.directionR.Z > 0,
			true
		};

		ref Node origin = ref nodes[0];

		do
		{
			uint index = (--next)->Index;
			ref readonly Node node = ref Unsafe.Add(ref origin, index);
			Float4 intersections = node.aabb4.Intersect(query.ray);

			if (orders[node.axisMajor])
			{
				if (orders[node.axisMinor1])
				{
					if (Push(intersections, node.token4, geometries, 3, ref next, ref query)) return true;
					if (Push(intersections, node.token4, geometries, 2, ref next, ref query)) return true;
				}
				else
				{
					if (Push(intersections, node.token4, geometries, 2, ref next, ref query)) return true;
					if (Push(intersections, node.token4, geometries, 3, ref next, ref query)) return true;
				}

				if (orders[node.axisMinor0])
				{
					if (Push(intersections, node.token4, geometries, 1, ref next, ref query)) return true;
					if (Push(intersections, node.token4, geometries, 0, ref next, ref query)) return true;
				}
				else
				{
					if (Push(intersections, node.token4, geometries, 0, ref next, ref query)) return true;
					if (Push(intersections, node.token4, geometries, 1, ref next, ref query)) return true;
				}
			}
			else
			{
				if (orders[node.axisMinor0])
				{
					if (Push(intersections, node.token4, geometries, 1, ref next, ref query)) return true;
					if (Push(intersections, node.token4, geometries, 0, ref next, ref query)) return true;
				}
				else
				{
					if (Push(intersections, node.token4, geometries, 0, ref next, ref query)) return true;
					if (Push(intersections, node.token4, geometries, 1, ref next, ref query)) return true;
				}

				if (orders[node.axisMinor1])
				{
					if (Push(intersections, node.token4, geometries, 3, ref next, ref query)) return true;
					if (Push(intersections, node.token4, geometries, 2, ref next, ref query)) return true;
				}
				else
				{
					if (Push(intersections, node.token4, geometries, 2, ref next, ref query)) return true;
					if (Push(intersections, node.token4, geometries, 3, ref next, ref query)) return true;
				}
			}

			[MethodImpl(ImplementationOptions)]
			static bool Push(in Float4 intersections, in EntityToken4 token4,
							 GeometryCollection geometries, int offset,
							 ref EntityToken* next, ref OccludeQuery query)
			{
				float hit = intersections[offset];
				if (hit >= query.travel) return false;

				ref readonly EntityToken token = ref token4[offset];

				if (token.Type.IsGeometry()) return geometries.Occlude(ref query, token); //Child is leaf

				//Child is branch
				*next++ = token;
				return false;
			}
		}
		while (next != stack);

		return false;
	}

	uint GetTraceCost(in EntityToken token, in Ray ray, ref float distance, float intersection = float.NegativeInfinity)
	{
		if (token.IsEmpty || intersection >= distance) return 0;
		if (token.Type.IsGeometry()) return geometries.GetTraceCost(ray, ref distance, token);

		ref readonly Node node = ref nodes[token.Index];
		Float4 intersections = node.aabb4.Intersect(ray);

		Span<bool> orders = stackalloc bool[Width]
		{
			ray.directionR.X > 0f,
			ray.directionR.Y > 0f,
			ray.directionR.Z > 0f,
			true
		};

		uint cost = Width;

		if (orders[node.axisMajor])
		{
			RecursiveTraceCost(orders[node.axisMinor0], 0, node.token4, ray, ref distance);
			RecursiveTraceCost(orders[node.axisMinor1], 2, node.token4, ray, ref distance);
		}
		else
		{
			RecursiveTraceCost(orders[node.axisMinor1], 2, node.token4, ray, ref distance);
			RecursiveTraceCost(orders[node.axisMinor0], 0, node.token4, ray, ref distance);
		}

		return cost;

		void RecursiveTraceCost(bool order, int offset, in EntityToken4 child4, in Ray ray, ref float distance)
		{
			if (order)
			{
				cost += GetTraceCost(child4[0 + offset], ray, ref distance, intersections[0 + offset]);
				cost += GetTraceCost(child4[1 + offset], ray, ref distance, intersections[1 + offset]);
			}
			else
			{
				cost += GetTraceCost(child4[1 + offset], ray, ref distance, intersections[1 + offset]);
				cost += GetTraceCost(child4[0 + offset], ray, ref distance, intersections[0 + offset]);
			}
		}
	}

	Node CreateNode(BuildNode buildNode, ReadOnlySpan<EntityToken> tokens, ref uint nodeIndex, out int depth)
	{
		Assert.IsNotNull(buildNode.child);
		BuildNode current = buildNode.child;

		Span<EntityToken> token4 = stackalloc EntityToken[Width];

		depth = 0;

		for (int i = 0; i < Width; i++)
		{
			EntityToken entityToken;
			int nodeDepth;

			if (current.IsEmpty)
			{
				entityToken = EntityToken.Empty;
				nodeDepth = 0;
			}
			else if (current.IsLeaf)
			{
				entityToken = tokens[current.source.index];
				nodeDepth = 1;
			}
			else
			{
				uint index = nodeIndex++;
				ref Node node = ref nodes[index];

				node = CreateNode(current, tokens, ref nodeIndex, out nodeDepth);
				entityToken = NewNodeToken(index);
			}

			token4[i] = entityToken;
			depth = Math.Max(depth, nodeDepth);

			current = current.sibling;
		}

		++depth;
		return new Node(buildNode, token4);
	}

	[StructLayout(LayoutKind.Explicit, Size = 128)] //Pad to 128 bytes for cache line
	readonly struct Node
	{
		public Node(BuildNode node, ReadOnlySpan<EntityToken> token4)
		{
			Assert.IsNotNull(node.child);
			BuildNode child = node.child;

			ref readonly var aabb0 = ref GetAABB(ref child);
			ref readonly var aabb1 = ref GetAABB(ref child);
			ref readonly var aabb2 = ref GetAABB(ref child);
			ref readonly var aabb3 = ref GetAABB(ref child);

			aabb4 = new AxisAlignedBoundingBox4(aabb0, aabb1, aabb2, aabb3);

			axisMajor = node.axisMajor;
			axisMinor0 = node.axisMinor0;
			axisMinor1 = node.axisMinor1;

			this.token4 = new EntityToken4(token4);

			static ref readonly AxisAlignedBoundingBox GetAABB(ref BuildNode node)
			{
				var source = node.source;
				node = node.sibling;

				if (source != null) return ref source.aabb;
				return ref AxisAlignedBoundingBox.none;
			}
		}

#if !RELEASE
		/// <summary>
		/// Static check to ensure our currently layout is correctly updated if <see cref="EntityToken4"/> size is changed.
		/// </summary>
		static unsafe Node()
		{
			if (sizeof(EntityToken4) is Width * EntityToken.Size and 16) return;
			throw new Exception($"Invalid layout or size for {nameof(Node)}!");
		}
#endif

		/// <summary>
		/// The <see cref="AxisAlignedBoundingBox"/>s of the four children contained in this <see cref="Node"/>.
		/// </summary>
		[FieldOffset(0)] public readonly AxisAlignedBoundingBox4 aabb4;

		/// <summary>
		/// The integer value of the axis that divided the two primary nodes.
		/// </summary>
		[FieldOffset(096)] public readonly int axisMajor;

		/// <summary>
		/// The integer value of the axis that divided the first two secondary nodes.
		/// </summary>
		[FieldOffset(100)] public readonly int axisMinor0;

		/// <summary>
		/// The integer value of the axis that divided the second two secondary nodes.
		/// </summary>
		[FieldOffset(104)] public readonly int axisMinor1;

		/// <summary>
		/// The <see cref="EntityToken"/> representing the four branches off from this <see cref="Node"/>,
		/// which is either a leaf geometry object or another <see cref="Node"/> child branch.
		/// </summary>
		[FieldOffset(108)] public readonly EntityToken4 token4;
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