using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using CodeHelpers.Collections;
using CodeHelpers.Mathematics;
using Echo.Core.Aggregation.Acceleration;
using Echo.Core.Aggregation.Selection;
using Echo.Core.Scenic.Geometries;
using Echo.Core.Scenic.Lights;
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
		lightPicker = LightPicker.Create(lights);
	}

	public readonly Accelerator accelerator;
	public readonly LightPicker lightPicker;

	public readonly GeometryCollection geometries;
	public readonly LightCollection lights;

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