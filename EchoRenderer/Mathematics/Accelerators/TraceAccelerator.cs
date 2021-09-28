using System;
using System.Collections.Generic;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using EchoRenderer.Mathematics.Intersections;

namespace EchoRenderer.Mathematics.Accelerators
{
	public abstract class TraceAccelerator
	{
		protected TraceAccelerator(PressedPack pack, IReadOnlyList<AxisAlignedBoundingBox> aabbs, IReadOnlyList<uint> tokens)
		{
			this.pack = pack;

			if (aabbs.Count != tokens.Count) throw ExceptionHelper.Invalid(nameof(aabbs), $"does not have a matching size with {nameof(tokens)}");
			if (aabbs.Count == 0) throw ExceptionHelper.Invalid(nameof(aabbs.Count), aabbs.Count, InvalidType.countIsZero);

#if DEBUG

			int count = tokens.Count;

			for (int i = 0; i < count; i++)
			{
				uint token = tokens[i];
				Assert.IsTrue(token < NodeThreshold);
			}

#endif
		}

		/// <summary>
		/// Computes and returns a unique hash value for this entire <see cref="TraceAccelerator"/>.
		/// Can be slow on large structures; may be used to compare construction between runtimes.
		/// </summary>
		public abstract int Hash { get; }

		protected readonly PressedPack pack;

		/// <summary>
		/// Any token passed into <see cref="TraceAccelerator"/> constructor must be smaller than this value.
		/// This value is used internally to differentiate between accelerator nodes and leaf nodes.
		/// </summary>
		public const uint NodeThreshold = 0x80000000u; //uint with the 32nd bit flipped on

		/// <summary>
		/// Traverses and finds the closest intersection of <paramref name="query"/> with this <see cref="TraceAccelerator"/>.
		/// The intersection is recorded in <paramref name="query"/>, and only intersections that are closer than the initial
		/// <paramref name="query.distance"/> value are tested.
		/// </summary>
		public abstract void GetIntersection(ref HitQuery query);

		/// <summary>
		/// Returns the number of intersection tests performed before a result it determined.
		/// NOTE that the returned value is the cost for a full query, not an occlusion query.
		/// </summary>
		public abstract int GetIntersectionCost(in Ray ray, ref float distance);

		/// <summary>
		/// Fills <paramref name="span"/> with the <see cref="AxisAlignedBoundingBox"/> of nodes in this <see cref="TraceAccelerator"/>
		/// at <paramref name="depth"/>, with the root node having a <paramref name="depth"/> of 1. Returns the actual length of
		/// <paramref name="span"/> used to store the <see cref="AxisAlignedBoundingBox"/>.
		/// NOTE: <paramref name="span"/> should not be shorter than 2 ^ (depth - 1).
		/// </summary>
		public abstract int FillAABB(int depth, Span<AxisAlignedBoundingBox> span);
	}
}