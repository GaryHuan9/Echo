using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Scenic;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Aggregation.Preparation;

/// <summary>
/// A <see cref="PreparedInstance"/> only used by <see cref="PreparedScene"/>, which is the root of the entire hierarchy.
/// </summary>
public class PreparedInstanceRoot : PreparedInstance
{
	public PreparedInstanceRoot(ScenePreparerOld preparer, Scene scene) : base(preparer, scene, null, EntityToken.Empty) { }

	/// <summary>
	/// Calculates and outputs the <see cref="AxisAlignedBoundingBox"/> and <see cref="BoundingSphere"/> of
	/// this <see cref="PreparedInstanceRoot"/> to <paramref name="aabb"/> and <paramref name="boundingSphere"/>.
	/// </summary>
	public void CalculateBounds(out AxisAlignedBoundingBox aabb, out BoundingSphere boundingSphere)
	{
		const int FetchDepth = 6; //How deep do we go into our accelerator to get the AABB of the nodes

		using var _0 = Pool<AxisAlignedBoundingBox>.Fetch(1 << FetchDepth, out var aabbs);

		SpanFill<AxisAlignedBoundingBox> fill = aabbs;
		pack.accelerator.FillBounds(FetchDepth, ref fill);
		aabbs = aabbs[..fill.Count];

		using var _1 = Pool<Float3>.Fetch(aabbs.Length * 8, out View<Float3> points);
		for (int i = 0; i < aabbs.Length; i++) aabbs[i].FillVertices(points[(i * 8)..]);

		aabb = new AxisAlignedBoundingBox(aabbs);
		boundingSphere = new BoundingSphere(points);
	}

	/// <summary>
	/// Interacts with the result of <paramref name="query"/> by returning an <see cref="Primitives.Touch"/>.
	/// </summary>
	public Touch Interact(in TraceQuery query)
	{
		query.AssertHit();

		PreparedInstance instance = this;
		var transform = Float4x4.identity;

		//Traverse down the instancing path
		foreach (ref readonly EntityToken nodeToken in query.token.Instances)
		{
			//Because we traverse in reverse, we must also multiply the transform in reverse
			transform = instance.inverseTransform * transform;
			instance = instance.pack.GetInstance(nodeToken);
		}

		return instance.pack.Interact(query, swatch, transform);
	}
}