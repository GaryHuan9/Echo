﻿using System;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;

namespace Echo.Core.Aggregation.Acceleration;

/// <summary>
/// A simple linear accelerator. Utilities four-wide SIMD parallelization.
/// Optimal for small numbers of geometries, but works with any length (including zero).
/// </summary>
public class LinearAccelerator : Accelerator
{
	public LinearAccelerator(GeometryCollection geometries, ReadOnlyView<Tokenized<BoxBound>> bounds) : base(geometries)
	{
		totalCount = bounds.Length;
		nodes = new Node[totalCount.CeiledDivide(Width)];

		for (int i = 0; i < nodes.Length; i++) nodes[i] = new Node(bounds[(i * Width)..]);
	}

	readonly Node[] nodes;
	readonly int totalCount;

	/// <summary>
	/// We store four <see cref="BoxBound"/> and tokens in one <see cref="Node"/>.
	/// </summary>
	const int Width = 4;

	public override void Trace(ref TraceQuery query)
	{
		foreach (ref readonly Node node in nodes.AsSpan())
		{
			Float4 intersections = node.bound4.Intersect(query.ray);

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
			Float4 intersections = node.bound4.Intersect(query.ray);

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
			Float4 intersections = node.bound4.Intersect(ray);

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

	public override void FillBounds(uint depth, ref SpanFill<BoxBound> fill)
	{
		fill.ThrowIfNotEmpty();

		//If theres enough room to store every individual bound
		if (fill.Length >= totalCount)
		{
			for (int i = 0; i < nodes.Length; i++)
			for (int j = 0; j < Width; j++)
			{
				if (fill.Count == totalCount) return;
				fill.Add(nodes[i].bound4[j]);
			}

			return;
		}

		//If there is enough space to store bounds that enclose every node's bound4
		if (fill.Length >= nodes.Length)
		{
			Span<BoxBound> bounds = stackalloc BoxBound[Width];

			for (int i = 0; i < nodes.Length; i++)
			{
				ref readonly Node node = ref nodes[i];
				int count = Math.Min(totalCount - i * Width, Width);

				if (count < Width)
				{
					for (int j = 0; j < count; j++) bounds[j] = node.bound4[j];
					fill.Add(new BoxBound(bounds[count..]));

					break;
				}

				fill.Add(node.bound4.Encapsulated);
			}

			return;
		}

		//Finally, store all enclosure bounds and then one last big bound that encloses all the remaining bounds
		while (fill.Count < fill.Length - 1)
		{
			ref readonly Node node = ref nodes[fill.Count];
			fill.Add(node.bound4.Encapsulated);
		}

		var builder = BoxBound.CreateBuilder();

		for (int i = fill.Count; i < nodes.Length; i++)
		{
			ref readonly Node node = ref nodes[i];
			int count = Math.Min(totalCount - i * Width, Width);

			if (count < Width)
			{
				for (int j = 0; j < count; j++) builder.Add(node.bound4[j]);
				break;
			}

			builder.Add(node.bound4.Encapsulated);
		}

		fill.Add(builder.ToBoxBound());
		Ensure.IsTrue(fill.IsFull);
	}

	readonly struct Node
	{
		public Node(ReadOnlySpan<Tokenized<BoxBound>> boundsSpan)
		{
			int length = boundsSpan.Length;

			bound4 = new BoxBound4
			(
				length > 0 ? boundsSpan[0].content : BoxBound.None,
				length > 1 ? boundsSpan[1].content : BoxBound.None,
				length > 2 ? boundsSpan[2].content : BoxBound.None,
				length > 3 ? boundsSpan[3].content : BoxBound.None
			);

			token4 = new EntityToken4
			(
				length > 0 ? boundsSpan[0].token : EntityToken.Empty,
				length > 1 ? boundsSpan[1].token : EntityToken.Empty,
				length > 2 ? boundsSpan[2].token : EntityToken.Empty,
				length > 3 ? boundsSpan[3].token : EntityToken.Empty
			);
		}

		public readonly BoxBound4 bound4;
		public readonly EntityToken4 token4;
	}
}