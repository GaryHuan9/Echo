using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Scenic.Geometric;
using Echo.Core.Scenic.Instancing;

namespace Echo.Core.Scenic.Preparation;

partial class ScenePreparerNew
{
	class Node
	{
		public Node(ScenePreparerNew preparer, EntityPack source)
		{
			var sources = new GeometrySources(source);

			var triangles = new PreparedTriangle[sources.counts.triangle];
			var instances = new PreparedInstance[sources.counts.instance];
			var spheres = new PreparedSphere[sources.counts.triangle];

			var swatchExtractor = new SwatchExtractor(preparer);

			FillFromSources(swatchExtractor, triangles, sources.triangles);
			FillFromSources(swatchExtractor, spheres, sources.spheres);

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

		static void FillFromSources<T>(SwatchExtractor swatchExtractor, T[] destination, ReadOnlySpan<IGeometrySource<T>> sources)
		{
			var fill = destination.AsFill();

			foreach (IGeometrySource<T> source in sources)
			{
				int start = fill.Count;

				foreach (T value in source.Extract(swatchExtractor)) fill.Add(value);

				int count = fill.Count - start;
				if (count == source.Count) continue;

				throw new Exception($"{nameof(IGeometrySource<T>.Count)} mismatch on {source}.");
			}

			Assert.IsTrue(fill.IsFull);
		}

		readonly ref struct GeometrySources
		{
			public GeometrySources(EntityPack source)
			{
				var triangleList = new List<IGeometrySource<PreparedTriangle>>();
				var sphereList = new List<IGeometrySource<PreparedSphere>>();
				var instanceList = new List<PackInstance>();

				ulong triangleCount = 0;
				ulong sphereCount = 0;
				ulong instanceCount = 0;

				foreach (Entity entity in source.LoopChildren(true))
				{
					if (entity is IGeometrySource<PreparedTriangle> triangle)
					{
						triangleList.Add(triangle);
						triangleCount += triangle.Count;
					}

					if (entity is IGeometrySource<PreparedSphere> sphere)
					{
						sphereList.Add(sphere);
						sphereCount += sphere.Count;
					}

					if (entity is PackInstance instance)
					{
						instanceList.Add(instance);
						++instanceCount;
					}
				}

				triangles = CollectionsMarshal.AsSpan(triangleList);
				spheres = CollectionsMarshal.AsSpan(sphereList);
				instances = CollectionsMarshal.AsSpan(instanceList);
				counts = new GeometryCounts(triangleCount, sphereCount, instanceCount);
			}

			public readonly ReadOnlySpan<IGeometrySource<PreparedTriangle>> triangles;
			public readonly ReadOnlySpan<IGeometrySource<PreparedSphere>> spheres;
			public readonly ReadOnlySpan<PackInstance> instances;
			public readonly GeometryCounts counts;
		}
	}
}