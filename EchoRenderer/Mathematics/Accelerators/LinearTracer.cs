using System;
using System.Collections.Generic;
using EchoRenderer.Mathematics.Intersections;

namespace EchoRenderer.Mathematics.Accelerators
{
	/// <summary>
	/// A simple linear search trace finder.
	/// </summary>
	public class LinearTracer : TraceAccelerator
	{
		public LinearTracer(PressedPack pack, IReadOnlyList<AxisAlignedBoundingBox> aabbs, IReadOnlyList<uint> tokens) : base(pack, aabbs, tokens) { }

		public override int Hash { get; }

		public override void Trace(ref TraceQuery query)
		{
			throw new NotImplementedException();
		}

		public override int TraceCost(in Ray ray, ref float distance) => throw new NotImplementedException();

		public override int FillAABB(uint depth, Span<AxisAlignedBoundingBox> span) => throw new NotImplementedException();
	}
}