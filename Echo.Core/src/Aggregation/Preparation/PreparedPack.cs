using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using Echo.Core.Aggregation.Acceleration;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Aggregation.Selection;
using Echo.Core.Common.Memory;
using Echo.Core.Scenic;
using Echo.Core.Scenic.Geometric;
using Echo.Core.Scenic.Instancing;
using Echo.Core.Scenic.Lighting;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Aggregation.Preparation;

public class PreparedPack
{
	public PreparedPack(SwatchExtractor swatchExtractor, ReadOnlySpan<IGeometrySource> geometrySources, ReadOnlySpan<ILightSource> lightSources)
	{
		var triangles = ExtractGeometries(geometrySources, swatchExtractor, out ReadOnlySpan<PreparedTriangle> emissiveTriangles);
		var spheres = ExtractGeometries(geometrySources, swatchExtractor, out ReadOnlySpan<PreparedSphere> emissiveSpheres);

		int length = (int)counts.Total;
		var aabbs = GC.AllocateUninitializedArray<AxisAlignedBoundingBox>(length);
		var tokens = GC.AllocateUninitializedArray<EntityToken>(length);

		for (int i = 0; i < triangles.Length; i++)
		{
			aabbs[i] = triangles[i].AABB;
			tokens[i] = new EntityToken(TokenType.Triangle, i);
		}

		int offset = triangles.Length;

		for (int i = 0; i < instances.Length; i++)
		{
			aabbs[offset + i] = instances[i].AABB;
			tokens[offset + i] = new EntityToken(TokenType.Instance, i);
		}

		offset += instances.Length;

		for (int i = 0; i < spheres.Length; i++)
		{
			aabbs[offset + i] = spheres[i].AABB;
			tokens[offset + i] = new EntityToken(TokenType.Sphere, i);
		}

		offset += spheres.Length;
		Assert.AreEqual(offset, length);

		accelerator = profile.CreateAggregator(this, aabbs, tokens);

		static T[] ExtractGeometries<T>(ReadOnlySpan<IGeometrySource> sources, SwatchExtractor extractor,
										out ReadOnlySpan<T> emissive) where T : IPreparedGeometry
		{
			int length = 0;

			foreach (IGeometrySource source in sources)
			{
				if (source is not IGeometrySource<T> match) continue;
				length += (int)match.Count;
			}

			if (length == 0)
			{
				emissive = ReadOnlySpan<T>.Empty;
				return null;
			}

			T[] array = GC.AllocateUninitializedArray<T>(length);

			int start = 0;
			int end = length - 1;

			foreach (IGeometrySource source in sources)
			{
				if (source is not IGeometrySource<T> match) continue;

				int gap = end - start;

				foreach (T value in match.Extract(extractor))
				{
					bool back = extractor.IsEmissive(value.Material);

					if (back) array[--end] = value;
					else array[++start] = value;

					if (start > end) break;
				}

				if (gap - match.Count == end - start) continue;
				throw new Exception($"{nameof(IGeometrySource<T>.Count)} mismatch on {source}.");
			}

			Assert.AreEqual(start, end + 1);
			emissive = array.AsSpan(start);
			return array;
		}
	}

	public readonly Accelerator accelerator;
	public readonly LightPicker lightPicker;

	protected readonly GeometryCollection geometries;
	protected readonly LightCollection lights;

	/// <summary>
	/// Creates a new <see cref="PreparedPack"/>; outputs the <paramref name="extractor"/> and
	/// <paramref name="tokens"/> that were used to construct this new <see cref="PreparedPack"/>.
	/// </summary>
	public static PreparedPack Create(ScenePreparer preparer, EntityPack pack, out SwatchExtractor extractor, out EntityTokenArray tokens)
	{
		//Collect objects
		var trianglesList = new ConcurrentList<PreparedTriangle>();
		var instancesList = new List<PreparedInstance>();
		var spheresList = new List<PreparedSphere>();

		extractor = new SwatchExtractor(preparer);

		trianglesList.BeginAdd();

		foreach (Entity child in pack.LoopChildren(true))
		{
			switch (child)
			{
				case GeometryEntity geometry:
				{
					trianglesList.AddRange(geometry.ExtractTriangles(extractor));
					spheresList.AddRange(geometry.ExtractSpheres(extractor));

					break;
				}
				case PackInstance objectInstance:
				{
					var token = new EntityToken(TokenType.Instance, instancesList.Count);
					var instance = new PreparedInstance(preparer, objectInstance, token);
					instancesList.Add(instance);

					break;
				}
			}
		}

		trianglesList.EndAdd();

		SubdivideTriangles(trianglesList, extractor, preparer.profile);

		//Extract prepared data
		var triangles = new PreparedTriangle[trianglesList.Count];
		var instances = new PreparedInstance[instancesList.Count];
		var spheres = new PreparedSphere[spheresList.Count];

		//Collect tokens
		tokens = CreateTokenArray(extractor, instances.Length);
		var aabbs = new AxisAlignedBoundingBox[tokens.TotalLength];

		var tokenArray = tokens;

		Parallel.For(0, triangles.Length, FillTriangles);
		Parallel.For(0, instances.Length, FillInstances);
		Parallel.For(0, spheres.Length, FillSpheres);

		Assert.IsTrue(tokens.IsFull);

		//Construct instance
		return new PreparedPack(preparer.profile.AggregatorProfile, aabbs, tokens, triangles, instances, spheres);

		void FillTriangles(int index)
		{
			ref readonly var triangle = ref trianglesList[index];

			triangles[index] = triangle;

			var token = new EntityToken(TokenType.Triangle, index);
			int at = tokenArray.Add(triangle.material, token);

			aabbs[at] = triangle.AABB;
		}

		void FillInstances(int index)
		{
			PreparedInstance instance = instances[index] = instancesList[index];
			int at = tokenArray.Add(tokenArray.FinalPartition, instance.token);

			Assert.AreEqual(instance.token.Type, TokenType.Instance);
			Assert.AreEqual((uint)index, instance.token.Index);

			aabbs[at] = instance.AABB;
		}

		void FillSpheres(int index)
		{
			var sphere = spheres[index] = spheresList[index];

			var token = new EntityToken(TokenType.Sphere, index);
			int at = tokenArray.Add(sphere.material, token);

			aabbs[at] = sphere.AABB;
		}
	}

	/// <summary>
	/// Divides large triangles for better space partitioning.
	/// </summary>
	static void SubdivideTriangles(ConcurrentList<PreparedTriangle> triangles, SwatchExtractor extractor, ScenePrepareProfile profile)
	{
		if (profile.FragmentationMaxIteration == 0) return;

		double totalArea = triangles.AsParallel().Sum(triangle => (double)triangle.Area);
		float threshold = (float)(totalArea / triangles.Count * profile.FragmentationThreshold);

		using (triangles.BeginAdd()) Parallel.For(0, triangles.Count, SubdivideSingle);

		void SubdivideSingle(int index)
		{
			ref PreparedTriangle triangle = ref triangles[index];
			int maxIteration = profile.FragmentationMaxIteration;

			int count = SubdivideTriangle(triangles, ref triangle, threshold, maxIteration);
			if (count > 0) extractor.Register(triangle.material, count);
		}
	}

	static int SubdivideTriangle(ConcurrentList<PreparedTriangle> triangles, ref PreparedTriangle triangle, float threshold, int maxIteration)
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

	static EntityTokenArray CreateTokenArray(SwatchExtractor extractor, int instanceCount)
	{
		bool hasInstance = instanceCount > 0;

		ReadOnlySpan<MaterialIndex> indices = extractor.Indices;
		int totalLength = indices.Length + (hasInstance ? 1 : 0);

		Span<int> lengths = stackalloc int[totalLength];
		if (hasInstance) lengths[^1] = instanceCount;

		foreach (MaterialIndex index in indices) lengths[index] = extractor.GetRegistrationCount(index);

		return new EntityTokenArray(lengths);
	}
}