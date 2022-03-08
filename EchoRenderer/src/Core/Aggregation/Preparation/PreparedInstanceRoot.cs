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
	public PreparedInstanceRoot(ScenePreparer preparer, Scene scene) : base(preparer, scene, null, NodeToken.empty) { }

	/// <summary>
	/// Calculates and outputs the <see cref="AxisAlignedBoundingBox"/> and <see cref="BoundingSphere"/> of
	/// this <see cref="PreparedInstanceRoot"/> to <paramref name="aabb"/> and <paramref name="boundingSphere"/>.
	/// </summary>
	public void CalculateBounds(out AxisAlignedBoundingBox aabb, out BoundingSphere boundingSphere)
	{
		const int FetchDepth = 6; //How deep do we go into our aggregator to get the AABB of the nodes

		using var _0 = Pool<AxisAlignedBoundingBox>.Fetch(1 << FetchDepth, out var aabbs);
		using var _1 = Pool<Float3>.Fetch(aabbs.Length * 8, out Span<Float3> points);

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

	/// <summary>
	/// Interacts with the result of <paramref name="query"/> by returning an <see cref="Interaction"/>.
	/// </summary>
	public Interaction Interact(in TraceQuery query)
	{
		query.AssertHit();

		PreparedInstance instance = this;
		var transform = Float4x4.identity;

		//Traverse down the instancing path
		foreach (ref readonly NodeToken nodeToken in query.token.Instances)
		{
			//Because we traverse in reverse, we must also multiply the transform in reverse
			transform = instance.inverseTransform * transform;
			instance = instance.pack.GetInstance(nodeToken);
		}

		return instance.pack.Interact(query, swatch, transform);
	}
}