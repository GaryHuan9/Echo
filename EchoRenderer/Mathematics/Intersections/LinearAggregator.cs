using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Primitives;

namespace EchoRenderer.Mathematics.Intersections
{
	/// <summary>
	/// A simple linear search trace finder.
	/// </summary>
	public class LinearAggregator : Aggregator
	{
		public LinearAggregator(PressedPack pack, ReadOnlyMemory<AxisAlignedBoundingBox> aabbs, ReadOnlySpan<Token> tokens) : base(pack)
		{
			Validate(aabbs, tokens);

			ReadOnlySpan<AxisAlignedBoundingBox> span = aabbs.Span;
			int length = span.Length.CeiledDivide(Width);

			for (int i = 0; i < tokens.Length; i++)
			{

			}
		}

		readonly Node[] nodes;

		/// <summary>
		/// We store four <see cref="AxisAlignedBoundingBox"/> and tokens in one <see cref="Node"/>.
		/// </summary>
		const int Width = 4;

		public override void Trace(ref TraceQuery query)
		{
			throw new NotImplementedException();
		}

		public override void Occlude(ref OccludeQuery query)
		{
			throw new NotImplementedException();
		}

		public override int TraceCost(in Ray ray, ref float distance) => throw new NotImplementedException();

		public override int GetHashCode() => 0;

		protected override int FillAABB(uint depth, Span<AxisAlignedBoundingBox> span) => throw new NotImplementedException();

		struct Node
		{
			public Node(Red)
			{

			}

			public readonly AxisAlignedBoundingBox4 aabb4;
			public readonly Token4 token4;
		}

		unsafe struct Token4
		{
			public Token4(ReadOnlySpan<uint> tokens)
			{
				int length = Math.Min(Width, tokens.Length);
				for (int i = 0; i < length; i++) data[i] = tokens[i];
				for (int i = length; i < Width; i++) data[i] = default;
			}

			fixed uint data[Width];

			public readonly uint this[int index] => data[index];
		}
	}
}