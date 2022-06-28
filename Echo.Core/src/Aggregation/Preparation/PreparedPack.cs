using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using CodeHelpers.Collections;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Acceleration;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Selection;
using Echo.Core.Common.Memory;
using Echo.Core.Scenic.Geometric;
using Echo.Core.Scenic.Lighting;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Aggregation.Preparation;

public class PreparedPack
{
	public PreparedPack(ReadOnlySpan<IGeometrySource> geometrySources, ReadOnlySpan<ILightSource> lightSources,
						ImmutableArray<PreparedInstance> instances, in AcceleratorCreator acceleratorCreator,
						SwatchExtractor swatchExtractor)
	{
		geometries = new GeometryCollection(swatchExtractor, geometrySources, instances);
		lights = new LightCollection(lightSources, geometries);
		accelerator = acceleratorCreator.Create(geometries);
		lightPicker = LightPicker.Create(lights, swatchExtractor.PrepareEmissive());
	}

	public readonly Accelerator accelerator;
	public readonly LightPicker lightPicker;

	public readonly GeometryCollection geometries;
	public readonly LightCollection lights;

	/// <summary>
	/// Calculates and outputs the <see cref="AxisAlignedBoundingBox"/> and <see cref="BoundingSphere"/> of
	/// this <see cref="PreparedPack"/> to <paramref name="aabb"/> and <paramref name="boundingSphere"/>.
	/// </summary>
	protected void CalculateBounds(out AxisAlignedBoundingBox aabb, out BoundingSphere boundingSphere)
	{
		const int FetchDepth = 6; //How deep do we go into our accelerator to get the AABB of the nodes

		using var _0 = Pool<AxisAlignedBoundingBox>.Fetch(1 << FetchDepth, out var aabbs);

		SpanFill<AxisAlignedBoundingBox> fill = aabbs;
		accelerator.FillBounds(FetchDepth, ref fill);
		aabbs = aabbs[..fill.Count];

		using var _1 = Pool<Float3>.Fetch(aabbs.Length * 8, out View<Float3> points);
		for (int i = 0; i < aabbs.Length; i++) aabbs[i].FillVertices(points[(i * 8)..]);

		aabb = new AxisAlignedBoundingBox(aabbs);
		boundingSphere = new BoundingSphere(points);
	}

	/// <summary>
	/// Divides large triangles for better space partitioning.
	/// </summary>
	static void SubdivideTriangles(ConcurrentList<PreparedTriangle> triangles, SwatchExtractor extractor, ScenePreparer profile)
	{
		if (profile.FragmentationMaxIteration == 0) return;

		double totalArea = triangles.AsParallel().Sum(triangle => (double)triangle.Area);
		float threshold = (float)(totalArea / triangles.Count * profile.FragmentationThreshold);

		using (triangles.BeginAdd()) Parallel.For(0, triangles.Count, SubdivideAt);

		void SubdivideAt(int index)
		{
			ref PreparedTriangle triangle = ref triangles[index];
			int maxIteration = profile.FragmentationMaxIteration;

			int count = SubdivideSingle(triangles, ref triangle, threshold, maxIteration);
			if (count > 0) extractor.Register(triangle.Material, count);
		}

		static int SubdivideSingle(ConcurrentList<PreparedTriangle> triangles, ref PreparedTriangle triangle, float threshold, int maxIteration)
		{
			float multiplier = MathF.Log2(triangle.Area / threshold);
			int iteration = Math.Min(multiplier.Ceil(), maxIteration);
			if (iteration <= 0) return 0;

			int count = 1 << (iteration * 2);
			Span<PreparedTriangle> divided = stackalloc PreparedTriangle[count];

			triangle.GetSubdivided(divided, iteration);
			triangle = divided[0];

			for (int i = 1; i < count; i++) triangles.Add(divided[i]);

			return count - 1;
		}
	}
}