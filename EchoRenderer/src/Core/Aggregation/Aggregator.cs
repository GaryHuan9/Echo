using System;
using System.Linq;
using System.Runtime.CompilerServices;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Preparation;
using EchoRenderer.Core.Aggregation.Primitives;

namespace EchoRenderer.Core.Aggregation;

/// <summary>
/// An aggregate of geometries and instances that is able to calculate intersections and handle queries in various ways.
/// </summary>
public abstract class Aggregator
{
	protected Aggregator(PreparedPack pack) => this.pack = pack;

	protected readonly PreparedPack pack;

	/// <summary>
	/// Calculates and returns the <see cref="AxisAlignedBoundingBox"/> of this <see cref="Aggregator"/> as if it was transformed
	/// by <paramref name="inverseTransform"/>. Note that this transformation is usually performed inversely, thus the name.
	/// </summary>
	public AxisAlignedBoundingBox GetTransformedAABB(in Float4x4 inverseTransform)
	{
		const int FetchDepth = 6; //How deep do we go into this aggregator to get the AABB of the nodes
		using var _ = SpanPool<AxisAlignedBoundingBox>.Fetch(1 << FetchDepth, out var aabbs);

		aabbs = aabbs[..FillAABB(FetchDepth, aabbs)];
		Float4x4 absoluteTransform = inverseTransform.Absoluted;

		Assert.IsFalse(aabbs.IsEmpty);

		Float3 min = Float3.positiveInfinity;
		Float3 max = Float3.negativeInfinity;

		//Potentially find a smaller AABB by encapsulating transformed children nodes instead of the full aggregator
		foreach (ref readonly AxisAlignedBoundingBox aabb in aabbs)
		{
			Float3 center = inverseTransform.MultiplyPoint(aabb.Center);
			Float3 extend = absoluteTransform.MultiplyDirection(aabb.Extend);

			min = min.Min(center - extend);
			max = max.Max(center + extend);
		}

		return new AxisAlignedBoundingBox(min, max);
	}

	/// <summary>
	/// Traverses and finds the closest intersection of <paramref name="query"/> with this <see cref="Aggregator"/>.
	/// The intersection is recorded in <paramref name="query"/>, and only intersections that are closer than the
	/// initial <paramref name="query.distance"/> value are tested.
	/// </summary>
	public abstract void Trace(ref TraceQuery query);

	/// <summary>
	/// Traverses this <see cref="Aggregator"/> and returns whether <paramref name="query"/> is occluded by anything.
	/// NOTE: this operation will terminate at any occlusion, so it will be more performant than <see cref="Trace"/>.
	/// </summary>
	public abstract bool Occlude(ref OccludeQuery query);

	/// <summary>
	/// Calculates and returns the number of approximated intersection tests performed before a result is determined.
	/// NOTE: the returned value is the cost for a full <see cref="TraceQuery"/>, not an <see cref="OccludeQuery"/>.
	/// </summary>
	public abstract int TraceCost(in Ray ray, ref float distance);

	/// <summary>
	/// Fills <paramref name="span"/> with the <see cref="AxisAlignedBoundingBox"/> of nodes in this <see cref="Aggregator"/>
	/// at <paramref name="depth"/>, with the root node having a <paramref name="depth"/> of 1. Returns the actual length of
	/// <paramref name="span"/> used to store the <see cref="AxisAlignedBoundingBox"/>. NOTE: <paramref name="span"/> should
	/// not be smaller than 2^depth, and the returned value will not be greater than <paramref name="span.Length"/>.
	/// </summary>
	public abstract int FillAABB(uint depth, Span<AxisAlignedBoundingBox> span);

	/// <summary>
	/// Validates that <paramref name="aabbs"/> and <paramref name="tokens"/>
	/// are allowed to be used to construct this <see cref="Aggregator"/>.
	/// </summary>
	protected static void Validate(ReadOnlyMemory<AxisAlignedBoundingBox> aabbs, ReadOnlySpan<NodeToken> tokens, Func<int, bool> lengthValidator = null)
	{
		if (aabbs.Length != tokens.Length) throw ExceptionHelper.Invalid(nameof(aabbs), $"does not have a matching length with {nameof(tokens)}");
		if (lengthValidator?.Invoke(tokens.Length) == false) throw ExceptionHelper.Invalid(nameof(tokens.Length), tokens.Length, "has invalid length");

#if DEBUG
			foreach (ref readonly NodeToken token in tokens) Assert.IsTrue(token.IsGeometry);
			foreach (ref readonly var aabb in aabbs.Span) Assert.IsFalse(aabb.min.EqualsExact(aabb.max));
#endif
	}

	/// <summary>
	/// Creates and returns an array of index indices from zero (inclusive) to <paramref name="max"/> (exclusive).
	/// </summary>
	protected static int[] CreateIndices(int max) => Enumerable.Range(0, max).ToArray();

	/// <summary>
	/// Swaps two <typeparamref name="T"/> pointers.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static unsafe void Swap<T>(ref T* pointer0, ref T* pointer1) where T : unmanaged
	{
		var storage = pointer0;
		pointer0 = pointer1;
		pointer1 = storage;
	}
}