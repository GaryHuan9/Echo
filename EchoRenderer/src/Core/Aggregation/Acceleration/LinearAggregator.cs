using System;
using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Preparation;
using EchoRenderer.Core.Aggregation.Primitives;

namespace EchoRenderer.Core.Aggregation.Acceleration;

/// <summary>
/// A simple linear aggregator. Utilities four-wide SIMD parallelization.
/// Optimal for small numbers of geometries and tokens, but works with any.
/// </summary>
public class LinearAggregator : Aggregator
{
	public LinearAggregator(PreparedPack pack, ReadOnlyView<AxisAlignedBoundingBox> aabbs, ReadOnlySpan<NodeToken> tokens) : base(pack)
	{
		Validate(aabbs, tokens);

		ReadOnlySpan<AxisAlignedBoundingBox> span = aabbs;
		nodes = new Node[span.Length.CeiledDivide(Width)];

		for (int i = 0; i < nodes.Length; i++)
		{
			int index = i * Width;

			nodes[i] = new Node(span[index..], tokens[index..]);
		}

		totalCount = span.Length;
	}

	readonly Node[] nodes;
	readonly int totalCount;

	/// <summary>
	/// We store four <see cref="AxisAlignedBoundingBox"/> and tokens in one <see cref="Node"/>.
	/// </summary>
	const int Width = 4;

	public override void Trace(ref TraceQuery query)
	{
		foreach (ref readonly Node node in nodes.AsSpan())
		{
			Vector128<float> intersections = node.aabb4.Intersect(query.ray);

			for (int i = 0; i < Width; i++)
			{
				if (intersections.GetElement(i) >= query.distance) continue;
				ref readonly NodeToken token = ref node.token4[i];
				pack.Trace(ref query, token);
			}
		}
	}

	public override bool Occlude(ref OccludeQuery query)
	{
		foreach (ref readonly Node node in nodes.AsSpan())
		{
			Vector128<float> intersections = node.aabb4.Intersect(query.ray);

			for (int i = 0; i < Width; i++)
			{
				if (intersections.GetElement(i) >= query.travel) continue;
				ref readonly NodeToken token = ref node.token4[i];
				if (pack.Occlude(ref query, token)) return true;
			}
		}

		return false;
	}

	public override int TraceCost(in Ray ray, ref float distance)
	{
		int cost = nodes.Length * Width;

		foreach (ref readonly Node node in nodes.AsSpan())
		{
			Vector128<float> intersections = node.aabb4.Intersect(ray);

			for (int i = 0; i < Width; i++)
			{
				if (intersections.GetElement(i) >= distance) continue;
				ref readonly NodeToken token = ref node.token4[i];
				cost += pack.GetTraceCost(ray, ref distance, token);
			}
		}

		return cost;
	}

	public override unsafe int GetHashCode()
	{
		fixed (Node* ptr = nodes) return Utilities.GetHashCode(ptr, (uint)nodes.Length, totalCount);
	}

	public override int FillAABB(uint depth, Span<AxisAlignedBoundingBox> span)
	{
		//If theres enough room to store every individual AABB
		if (span.Length >= totalCount)
		{
			var fill = span[..totalCount].AsFill();

			foreach (ref readonly Node node in nodes.AsSpan())
			{
				for (int i = 0; i < Width; i++)
				{
					if (fill.IsFull) goto exit;
					fill.Add(node.aabb4[i]);
				}
			}

		exit:
			return totalCount;
		}

		Span<AxisAlignedBoundingBox> aabb4 = stackalloc AxisAlignedBoundingBox[Width];

		//If there is enough space to store AABBs that enclose every node's AABB4
		if (span.Length >= nodes.Length)
		{
			for (int i = 0; i < nodes.Length; i++)
			{
				ref readonly Node node = ref nodes[i];

				int count = Math.Min(totalCount - i * Width, Width);
				Span<AxisAlignedBoundingBox> slice = aabb4[count..];

				for (int j = 0; j < count; j++) slice[j] = node.aabb4[j];

				span[i] = new AxisAlignedBoundingBox(slice);
			}

			return nodes.Length;
		}

		//Finally, store all enclosure AABBs and then one last big AABB that encloses all the remaining AABBs
		for (int i = 0; i < span.Length - 1; i++)
		{
			ref readonly Node node = ref nodes[i];

			for (int j = 0; j < Width; j++) aabb4[j] = node.aabb4[j];

			span[i] = new AxisAlignedBoundingBox(aabb4);
		}

		Float3 min = Float3.positiveInfinity;
		Float3 max = Float3.negativeInfinity;

		for (int i = span.Length; i < nodes.Length; i++)
		{
			ref readonly Node node = ref nodes[i];

			int count = Math.Min(totalCount - i * Width, Width);

			for (int j = 0; j < count; j++)
			{
				AxisAlignedBoundingBox aabb = node.aabb4[j];

				min = min.Min(aabb.min);
				max = max.Max(aabb.max);
			}
		}

		span[^1] = new AxisAlignedBoundingBox(min, max);

		return span.Length;
	}

	readonly struct Node
	{
		public Node(ReadOnlySpan<AxisAlignedBoundingBox> aabbs, ReadOnlySpan<NodeToken> tokens)
		{
			aabb4 = new AxisAlignedBoundingBox4(aabbs);
			token4 = new NodeToken4(tokens);
		}

		public readonly AxisAlignedBoundingBox4 aabb4;
		public readonly NodeToken4 token4;
	}
}