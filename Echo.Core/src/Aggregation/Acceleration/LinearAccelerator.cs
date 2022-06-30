using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common;
using Echo.Core.Common.Memory;

namespace Echo.Core.Aggregation.Acceleration;

/// <summary>
/// A simple linear accelerator. Utilities four-wide SIMD parallelization.
/// Optimal for small numbers of geometries, but works with any length (including zero).
/// </summary>
public class LinearAccelerator : Accelerator
{
	public LinearAccelerator(GeometryCollection geometries, ReadOnlyView<Tokenized<AxisAlignedBoundingBox>> boundsView) : base(geometries)
	{
		totalCount = boundsView.Length;
		nodes = new Node[totalCount.CeiledDivide(Width)];

		for (int i = 0; i < nodes.Length; i++) nodes[i] = new Node(boundsView[(i * Width)..]);
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
			Float4 intersections = node.aabb4.Intersect(query.ray);

			for (int i = 0; i < Width; i++)
			{
				if (intersections[i] >= query.distance) continue;
				geometries.Trace(node.token4[i], ref query);
			}
		}
	}

	public override bool Occlude(ref OccludeQuery query)
	{
		foreach (ref readonly Node node in nodes.AsSpan())
		{
			Float4 intersections = node.aabb4.Intersect(query.ray);

			for (int i = 0; i < Width; i++)
			{
				if (intersections[i] >= query.travel) continue;
				if (geometries.Occlude(node.token4[i], ref query)) return true;
			}
		}

		return false;
	}

	public override uint TraceCost(in Ray ray, ref float distance)
	{
		uint cost = (uint)nodes.Length * Width;

		foreach (ref readonly Node node in nodes.AsSpan())
		{
			Float4 intersections = node.aabb4.Intersect(ray);

			for (int i = 0; i < Width; i++)
			{
				if (intersections[i] >= distance) continue;
				cost += geometries.GetTraceCost(ray, ref distance, node.token4[i]);
			}
		}

		return cost;
	}

	public override unsafe int GetHashCode()
	{
		fixed (Node* ptr = nodes) return Utility.GetHashCode(ptr, (uint)nodes.Length, totalCount);
	}

	public override void FillBounds(uint depth, ref SpanFill<AxisAlignedBoundingBox> fill)
	{
		fill.ThrowIfNotEmpty();

		//If theres enough room to store every individual AABB
		if (fill.Length >= totalCount)
		{
			for (int i = 0; i < nodes.Length; i++)
			for (int j = 0; j < Width; j++)
			{
				if (fill.Count == totalCount) return;
				fill.Add(nodes[i].aabb4[j]);
			}

			return;
		}

		//If there is enough space to store AABBs that enclose every node's AABB4
		if (fill.Length >= nodes.Length)
		{
			Span<AxisAlignedBoundingBox> aabb4 = stackalloc AxisAlignedBoundingBox[Width];

			for (int i = 0; i < nodes.Length; i++)
			{
				ref readonly Node node = ref nodes[i];
				int count = Math.Min(totalCount - i * Width, Width);

				if (count < Width)
				{
					for (int j = 0; j < count; j++) aabb4[j] = node.aabb4[j];
					fill.Add(new AxisAlignedBoundingBox(aabb4[count..]));

					break;
				}

				fill.Add(node.aabb4.Encapsulated);
			}

			return;
		}

		//Finally, store all enclosure AABBs and then one last big AABB that encloses all the remaining AABBs
		while (fill.Count < fill.Length - 1)
		{
			ref readonly Node node = ref nodes[fill.Count];
			fill.Add(node.aabb4.Encapsulated);
		}

		var min = Float3.PositiveInfinity;
		var max = Float3.NegativeInfinity;

		for (int i = fill.Count; i < nodes.Length; i++)
		{
			ref readonly Node node = ref nodes[i];
			int count = Math.Min(totalCount - i * Width, Width);

			if (count < Width)
			{
				for (int j = 0; j < count; j++) Encapsulate(node.aabb4[j]);
				break;
			}

			Encapsulate(node.aabb4.Encapsulated);

			void Encapsulate(in AxisAlignedBoundingBox aabb)
			{
				min = min.Min(aabb.min);
				max = max.Max(aabb.max);
			}
		}

		fill.Add(new AxisAlignedBoundingBox(min, max));
		Assert.IsTrue(fill.IsFull);
	}

	readonly struct Node
	{
		public Node(ReadOnlySpan<Tokenized<AxisAlignedBoundingBox>> boundsSpan)
		{
			int length = boundsSpan.Length;

			aabb4 = new AxisAlignedBoundingBox4
			(
				length > 0 ? boundsSpan[0].content : AxisAlignedBoundingBox.none,
				length > 1 ? boundsSpan[1].content : AxisAlignedBoundingBox.none,
				length > 2 ? boundsSpan[2].content : AxisAlignedBoundingBox.none,
				length > 3 ? boundsSpan[3].content : AxisAlignedBoundingBox.none
			);

			token4 = new EntityToken4
			(
				length > 0 ? boundsSpan[0].token : EntityToken.Empty,
				length > 1 ? boundsSpan[1].token : EntityToken.Empty,
				length > 2 ? boundsSpan[2].token : EntityToken.Empty,
				length > 3 ? boundsSpan[3].token : EntityToken.Empty
			);
		}

		public readonly AxisAlignedBoundingBox4 aabb4;
		public readonly EntityToken4 token4;
	}
}