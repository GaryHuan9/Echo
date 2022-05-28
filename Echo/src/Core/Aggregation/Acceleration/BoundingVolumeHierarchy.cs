using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeHelpers.Diagnostics;
using Echo.Common;
using Echo.Common.Mathematics.Primitives;
using Echo.Common.Memory;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;

namespace Echo.Core.Aggregation.Acceleration;

/// <summary>
/// A binary hierarchical spacial partitioning acceleration structure.
/// Works best with medium-level quantities of geometries and tokens.
/// There must be more than one token and <see cref="AxisAlignedBoundingBox"/> to process.
/// </summary>
public class BoundingVolumeHierarchy : Aggregator
{
	public BoundingVolumeHierarchy(PreparedPack pack, ReadOnlyView<AxisAlignedBoundingBox> aabbs, ReadOnlySpan<NodeToken> tokens) : base(pack)
	{
		Validate(aabbs, tokens, length => length > 1);
		int[] indices = CreateIndices(aabbs.Length);

		BranchBuilder builder = new BranchBuilder(aabbs);
		BranchBuilder.Node root = builder.Build(indices);

		uint nodeIndex = 1;

		nodes = new Node[indices.Length * 2 - 1];
		nodes[0] = CreateNode(root, tokens, ref nodeIndex, out maxDepth);

		Assert.AreEqual((long)nodeIndex, nodes.Length);
	}

	readonly Node[] nodes;
	readonly int maxDepth;

	public override void Trace(ref TraceQuery query)
	{
		ref readonly Node root = ref nodes[0];
		float hit = root.aabb.Intersect(query.ray);

		if (hit < query.distance) TraceImpl(ref query);
	}

	public override bool Occlude(ref OccludeQuery query)
	{
		ref readonly Node root = ref nodes[0];
		float hit = root.aabb.Intersect(query.ray);

		return hit <= query.travel && OccludeImpl(ref query);
	}

	public override int TraceCost(in Ray ray, ref float distance)
	{
		ref readonly Node root = ref nodes[0];
		float hit = root.aabb.Intersect(ray);

		if (hit >= distance) return 1;
		return GetTraceCost(root.token, ray, ref distance) + 1;
	}

	public override unsafe int GetHashCode()
	{
		fixed (Node* ptr = nodes) return Utilities.GetHashCode(ptr, (uint)nodes.Length, maxDepth);
	}

	public override unsafe void FillAABB(uint depth, ref SpanFill<AxisAlignedBoundingBox> fill)
	{
		int length = 1 << (int)depth;
		fill.ThrowIfNotEmpty();
		fill.ThrowIfTooSmall(length);

		NodeToken* stack0 = stackalloc NodeToken[length];
		NodeToken* stack1 = stackalloc NodeToken[length];

		NodeToken* next0 = stack0;
		NodeToken* next1 = stack1;

		*next0++ = NodeToken.Root;

		for (uint i = 0; i < depth; i++)
		{
			while (next0 != stack0)
			{
				uint index = (--next0)->NodeValue;
				ref readonly Node node = ref nodes[index];

				if (node.token.IsNode)
				{
					*next1++ = node.token;
					*next1++ = node.token.Next;
				}
				else fill.Add(node.aabb); //If leaf then we write it to the result
			}

			//Swap the two stacks
			Swap(ref next0, ref next1);
			Swap(ref stack0, ref stack1);
		}

		//Export results
		while (next0 != stack0)
		{
			ref readonly Node node = ref nodes[(--next0)->NodeValue];
			fill.Add(node.aabb);
		}
	}

	[SkipLocalsInit]
	[MethodImpl(ImplementationOptions)]
	unsafe void TraceImpl(ref TraceQuery query)
	{
		var stack = stackalloc NodeToken[maxDepth];
		float* hits = stackalloc float[maxDepth];

		NodeToken* next = stack; //A pointer pointing at the top of the stack

		*next++ = NodeToken.Root.Next;    //We have already tested with the root before this method is invoked
		*hits++ = float.NegativeInfinity; //Explicitly initialize intersection distance

		do
		{
			uint index = (--next)->NodeValue;
			if (*--hits >= query.distance) continue;

			ref readonly Node child0 = ref nodes[index];
			ref readonly Node child1 = ref nodes[index + 1];

			float hit0 = child0.aabb.Intersect(query.ray);
			float hit1 = child1.aabb.Intersect(query.ray);

			//Orderly intersects the two children so that there is a higher chance of intersection on the first child.
			//Although the order of leaf intersection is wrong, the performance is actually better than reversing to correct it.

			if (hit0 < hit1)
			{
				Push(hit1, child1.token, ref query);
				Push(hit0, child0.token, ref query);
			}
			else
			{
				Push(hit0, child0.token, ref query);
				Push(hit1, child1.token, ref query);
			}

			[MethodImpl(ImplementationOptions)]
			void Push(float hit, in NodeToken token, ref TraceQuery refQuery)
			{
				if (hit >= refQuery.distance) return;

				if (token.IsNode)
				{
					//Child is branch
					*next++ = token;
					*hits++ = hit;
				}
				else pack.Trace(ref refQuery, token);
			}
		}
		while (next != stack);
	}

	[SkipLocalsInit]
	[MethodImpl(ImplementationOptions)]
	unsafe bool OccludeImpl(ref OccludeQuery query)
	{
		NodeToken* stack = stackalloc NodeToken[maxDepth];
		NodeToken* next = stack; //A pointer pointing at the top of the stack

		*next++ = NodeToken.Root.Next; //Push the child of the root because we have already tested with the root

		do
		{
			uint index = (--next)->NodeValue;

			ref readonly Node child0 = ref nodes[index];
			ref readonly Node child1 = ref nodes[index + 1];

			float hit0 = child0.aabb.Intersect(query.ray);
			float hit1 = child1.aabb.Intersect(query.ray);

			//Orderly intersects the two children so that there is a higher chance of intersection on the first child.
			//Although the order of leaf intersection is wrong, the performance is actually better than reversing to correct it.

			if (hit0 < hit1)
			{
				if (Push(hit1, child1.token, ref query)) return true;
				if (Push(hit0, child0.token, ref query)) return true;
			}
			else
			{
				if (Push(hit0, child0.token, ref query)) return true;
				if (Push(hit1, child1.token, ref query)) return true;
			}

			[MethodImpl(ImplementationOptions)]
			bool Push(float hit, in NodeToken token, ref OccludeQuery refQuery)
			{
				if (hit >= refQuery.travel) return false;

				if (token.IsNode)
				{
					//Add branch
					*next++ = token;
					return false;
				}

				//Evaluate leaf
				return pack.Occlude(ref refQuery, token);
			}
		}
		while (next != stack);

		return false;
	}

	int GetTraceCost(in NodeToken token, in Ray ray, ref float distance)
	{
		if (token.IsGeometry)
		{
			//Calculate the intersection cost on the leaf
			return pack.GetTraceCost(ray, ref distance, token);
		}

		uint index = token.NodeValue;

		ref readonly Node child0 = ref nodes[index];
		ref readonly Node child1 = ref nodes[index + 1];

		float hit0 = child0.aabb.Intersect(ray);
		float hit1 = child1.aabb.Intersect(ray);

		int cost = 2;

		//Orderly intersects the two children so that there is a higher chance of intersection on the first child
		if (hit0 < hit1)
		{
			if (hit0 < distance) cost += GetTraceCost(child0.token, ray, ref distance);
			if (hit1 < distance) cost += GetTraceCost(child1.token, ray, ref distance);
		}
		else
		{
			if (hit1 < distance) cost += GetTraceCost(child1.token, ray, ref distance);
			if (hit0 < distance) cost += GetTraceCost(child0.token, ray, ref distance);
		}

		return cost;
	}

	Node CreateNode(BranchBuilder.Node node, ReadOnlySpan<NodeToken> tokens, ref uint nodeIndex, out int depth)
	{
		if (node.IsLeaf)
		{
			depth = 1;
			return new Node(node.aabb, tokens[node.index]);
		}

		uint child0 = nodeIndex;
		uint child1 = child0 + 1;
		nodeIndex += 2;

		nodes[child0] = CreateNode(node.child0, tokens, ref nodeIndex, out int depth0);
		nodes[child1] = CreateNode(node.child1, tokens, ref nodeIndex, out int depth1);

		depth = Math.Max(depth0, depth1) + 1;
		return new Node(node.aabb, NodeToken.CreateNode(child0));
	}

	[StructLayout(LayoutKind.Explicit, Size = 32)] //Size must be under 32 bytes to fit two nodes in one cache line (64 bytes)
	readonly struct Node
	{
		public Node(in AxisAlignedBoundingBox aabb, in NodeToken token)
		{
			this.aabb = aabb;
			this.token = token;
		}

		//NOTE: the AABB is 28 bytes large, but its last 4 bytes are not used and only occupied for SIMD loading
		//So we can overlap the next four bytes onto the AABB and pay extra attention when first assigning the fields
		//This technique is currently not used here

		[FieldOffset(0)] public readonly AxisAlignedBoundingBox aabb;

		/// <summary>
		/// This is the <see cref="NodeToken"/> stored in this <see cref="Node"/>, which might represent either the leaf geometry if this <see cref="NodeToken.IsGeometry"/>,
		/// or the index of the first child of this <see cref="Node"/> if <see cref="NodeToken.IsNode"/> (and the second child can be accessed using <see cref="NodeToken.Next"/>.
		/// </summary>
		[FieldOffset(24)] public readonly NodeToken token;
	}
}