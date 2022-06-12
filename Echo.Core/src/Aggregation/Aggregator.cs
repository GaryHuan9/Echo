using System;
using System.Linq;
using System.Runtime.CompilerServices;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;

namespace Echo.Core.Aggregation;

/// <summary>
/// An aggregate of geometries and instances that is able to calculate intersections and handle queries in various ways.
/// </summary>
public abstract class Aggregator
{
	protected Aggregator(PreparedPack pack) => this.pack = pack;

	protected readonly PreparedPack pack;

	protected const MethodImplOptions ImplementationOptions = MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining;

	/// <summary>
	/// Calculates and returns the <see cref="AxisAlignedBoundingBox"/> of this <see cref="Aggregator"/> as if it was transformed
	/// by <paramref name="inverseTransform"/>. Note that this transformation is usually performed inversely, thus the name.
	/// </summary>
	public AxisAlignedBoundingBox GetTransformedAABB(in Float4x4 inverseTransform)
	{
		const int FetchDepth = 6; //How deep do we go into this aggregator to get the AABB of the nodes
		using var _ = Pool<AxisAlignedBoundingBox>.Fetch(1 << FetchDepth, out var aabbs);

		SpanFill<AxisAlignedBoundingBox> fill = aabbs;
		pack.aggregator.FillAABB(FetchDepth, ref fill);
		aabbs = aabbs[..fill.Count];
		Assert.IsFalse(aabbs.IsEmpty);

		Float4x4 absoluteTransform = inverseTransform.Absoluted;

		Float3 min = Float3.PositiveInfinity;
		Float3 max = Float3.NegativeInfinity;

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
	/// Fills a <see cref="SpanFill{T}"/> with the <see cref="AxisAlignedBoundingBox"/> in this <see cref="Aggregator"/>.
	/// </summary>
	/// <param name="depth">How deep to gather all of the <see cref="AxisAlignedBoundingBox"/>s (with the root node at 1).</param>
	/// <param name="fill">The destination <see cref="SpanFill{T}"/>; it should not be smaller than 2^<paramref name="depth"/>.</param>
	/// <remarks>It is guaranteed that the entirety of this <see cref="Aggregator"/> will be
	/// enclosed by all the <see cref="AxisAlignedBoundingBox"/>s that are filled.</remarks>
	public abstract void FillAABB(uint depth, ref SpanFill<AxisAlignedBoundingBox> fill);

	/// <summary>
	/// Validates that <paramref name="aabbs"/> and <paramref name="tokens"/>
	/// are allowed to be used to construct this <see cref="Aggregator"/>.
	/// </summary>
	protected static void Validate(ReadOnlyView<AxisAlignedBoundingBox> aabbs, ReadOnlySpan<NodeToken> tokens, Func<int, bool> lengthValidator = null)
	{
		if (aabbs.Length != tokens.Length) throw ExceptionHelper.Invalid(nameof(aabbs), $"does not have a matching length with {nameof(tokens)}");
		if (lengthValidator?.Invoke(tokens.Length) == false) throw ExceptionHelper.Invalid(nameof(tokens.Length), tokens.Length, "has invalid length");

#if DEBUG
		foreach (ref readonly NodeToken token in tokens) Assert.IsTrue(token.IsGeometry);
		foreach (ref readonly var aabb in aabbs) Assert.IsFalse(aabb.min.EqualsExact(aabb.max));
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