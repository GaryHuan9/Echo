using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Scenic;
using EchoRenderer.Core.Scenic.Preparation;

namespace EchoRenderer.Core.Aggregation.Preparation;

/// <summary>
/// A <see cref="PreparedInstance"/> only used by <see cref="PreparedScene"/>, which is the root of the entire hierarchy.
/// </summary>
public class PreparedInstanceRoot : PreparedInstance
{
	public PreparedInstanceRoot(ScenePreparer preparer, Scene scene) : base(preparer, scene, null, uint.MaxValue) { }

	/// <summary>
	/// Calculates and outputs the <see cref="AxisAlignedBoundingBox"/> and <see cref="BoundingSphere"/> of
	/// this <see cref="PreparedInstanceRoot"/> to <paramref name="aabb"/> and <paramref name="boundingSphere"/>.
	/// </summary>
	public void CalculateBounds(out AxisAlignedBoundingBox aabb, out BoundingSphere boundingSphere)
	{
		const int FetchDepth = 6; //How deep do we go into our aggregator to get the AABB of the nodes

		using var _0 = SpanPool<AxisAlignedBoundingBox>.Fetch(1 << FetchDepth, out var aabbs);
		using var _1 = SpanPool<Float3>.Fetch(aabbs.Length * 8, out Span<Float3> points);

		aabbs = aabbs[..pack.aggregator.FillAABB(FetchDepth, aabbs)];

		for (int i = 0; i < aabbs.Length; i++) aabbs[i].FillVertices(points[(i * 8)..]);

		aabb = new AxisAlignedBoundingBox(aabbs);
		boundingSphere = new BoundingSphere(points);
	}

	/// <summary>
	/// Processes <paramref name="query"/> as a <see cref="PreparedInstance"/> root.
	/// </summary>
	public void TraceRoot(ref TraceQuery query)
	{
		Assert.AreEqual(query.current, default);
		pack.aggregator.Trace(ref query);
	}

	/// <summary>
	/// Processes <paramref name="query"/> as a <see cref="PreparedInstance"/> root and returns the result.
	/// </summary>
	public bool OccludeRoot(ref OccludeQuery query)
	{
		Assert.AreEqual(query.current, default);
		return pack.aggregator.Occlude(ref query);
	}
}