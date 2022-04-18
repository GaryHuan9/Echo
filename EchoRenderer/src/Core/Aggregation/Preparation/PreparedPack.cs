using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Evaluation.Distributions;
using EchoRenderer.Core.Evaluation.Materials;
using EchoRenderer.Core.Scenic;
using EchoRenderer.Core.Scenic.Geometries;
using EchoRenderer.Core.Scenic.Instancing;
using EchoRenderer.Core.Scenic.Preparation;

namespace EchoRenderer.Core.Aggregation.Preparation;

public class PreparedPack
{
	PreparedPack(AggregatorProfile profile, ReadOnlyView<AxisAlignedBoundingBox> aabbs, ReadOnlySpan<NodeToken> tokens,
				 PreparedTriangle[] triangles, PreparedSphere[] spheres, PreparedInstance[] instances)
	{
		counts = new GeometryCounts(triangles.Length, spheres.Length, instances.Length);
		aggregator = profile.CreateAggregator(this, aabbs, tokens);

		this.triangles = triangles;
		this.spheres = spheres;
		this.instances = instances;
	}

	public readonly Aggregator aggregator;
	public readonly GeometryCounts counts;

	readonly PreparedTriangle[] triangles;
	readonly PreparedSphere[] spheres;
	readonly PreparedInstance[] instances;

	/// <summary>
	/// Calculates the intersection between <paramref name="query"/> and the object represented by <paramref name="token"/>.
	/// The intersection is only recorded if it occurs before the original <paramref name="query.distance"/>.
	/// </summary>
	public void Trace(ref TraceQuery query, in NodeToken token)
	{
		Assert.IsTrue(token.IsGeometry);
		query.current.Geometry = token;

		if (token.IsTriangle)
		{
			if (query.ignore == query.current) return;

			ref readonly var triangle = ref triangles[token.TriangleValue];
			float distance = triangle.Intersect(query.ray, out Float2 uv);

			if (distance >= query.distance) return;

			query.token = query.current;
			query.distance = distance;
			query.uv = uv;
		}
		else if (token.IsSphere)
		{
			bool findFar = query.ignore == query.current;

			ref readonly PreparedSphere sphere = ref spheres[token.SphereValue];
			float distance = sphere.Intersect(query.ray, out Float2 uv, findFar);

			if (distance >= query.distance) return;

			query.token = query.current;
			query.distance = distance;
			query.uv = uv;
		}
		else instances[token.InstanceValue].Trace(ref query);
	}

	/// <summary>
	/// Calculates and returns whether <paramref name="query"/> is occluded by the object represented by <paramref name="token"/>.
	/// </summary>
	public bool Occlude(ref OccludeQuery query, in NodeToken token)
	{
		Assert.IsTrue(token.IsGeometry);
		query.current.Geometry = token;

		if (token.IsTriangle)
		{
			if (query.ignore == query.current) return false;

			ref readonly var triangle = ref triangles[token.TriangleValue];
			return triangle.Intersect(query.ray, query.travel);
		}

		if (token.IsSphere)
		{
			bool findFar = query.ignore == query.current;

			ref readonly var sphere = ref spheres[token.SphereValue];
			return sphere.Intersect(query.ray, query.travel, findFar);
		}

		return instances[token.InstanceValue].Occlude(ref query);
	}

	/// <summary>
	/// Creates a new <see cref="Touch"/> from <paramref name="query"/>. The <see cref="Material"/> is extracted from <paramref name="swatch"/>.
	/// The world to local <see cref="Float4x4"/> for the geometry found with <paramref name="query"/> should be passed through <paramref name="transform"/>.
	/// </summary>
	public Touch Interact(in TraceQuery query, PreparedSwatch swatch, in Float4x4 transform)
	{
		NodeToken token = query.token.Geometry;
		Assert.IsTrue(token.IsGeometry);

		Float3 normal;
		Float2 texcoord;
		MaterialIndex materialIndex;

		if (token.IsTriangle)
		{
			ref readonly var triangle = ref triangles[token.TriangleValue];

			normal = triangle.GetNormal(query.uv);
			texcoord = triangle.GetTexcoord(query.uv);
			materialIndex = triangle.material;
		}
		else if (token.IsSphere)
		{
			ref readonly var sphere = ref spheres[token.SphereValue];

			normal = PreparedSphere.GetNormal(query.uv);
			texcoord = PreparedSphere.GetTexcoord(query.uv);
			materialIndex = sphere.material;
		}
		else throw NotBasePackException();

		normal = transform.MultiplyDirection(normal).Normalized; //Apply world transform to normal
		Material material = swatch[materialIndex];               //Find appropriate mapped material

		//Construct touch
		if (material == null) return new Touch(query, normal);
		return new Touch(query, normal, material, texcoord);
	}

	/// <summary>
	/// Returns the <see cref="PreparedInstance"/> stored in this <see cref="PreparedPack"/> represented by <paramref name="token"/>.
	/// </summary>
	public PreparedInstance GetInstance(in NodeToken token) => instances[token.InstanceValue];

	/// <summary>
	/// Returns the <see cref="MaterialIndex"/> of the geometry represented by <paramref name="token"/>.
	/// </summary>
	public MaterialIndex GetMaterialIndex(in NodeToken token)
	{
		if (token.IsTriangle)
		{
			ref readonly var triangle = ref triangles[token.TriangleValue];
			return triangle.material;
		}

		if (token.IsSphere)
		{
			ref readonly var sphere = ref spheres[token.SphereValue];
			return sphere.material;
		}

		throw NotBasePackException();
	}

	/// <summary>
	/// Returns the area of the geometry represented by <paramref name="token"/>.
	/// </summary>
	public float GetArea(in NodeToken token)
	{
		Assert.IsTrue(token.IsGeometry);

		if (token.IsTriangle)
		{
			ref readonly var triangle = ref triangles[token.TriangleValue];
			return triangle.Area;
		}

		if (token.IsSphere)
		{
			ref readonly var sphere = ref spheres[token.SphereValue];
			return sphere.Area;
		}

		throw NotBasePackException();
	}

	/// <summary>
	/// Underlying implementation of <see cref="PreparedScene.Sample"/>, functional
	/// according to the local coordinate system of this <see cref="PreparedPack"/>.
	/// </summary>
	public Probable<GeometryPoint> Sample(in NodeToken token, in Float3 origin, Sample2D sample)
	{
		Assert.IsTrue(token.IsGeometry);

		if (token.IsTriangle)
		{
			ref readonly var triangle = ref triangles[token.TriangleValue];
			return triangle.Sample(origin, sample);
		}

		if (token.IsSphere)
		{
			ref readonly var sphere = ref spheres[token.SphereValue];
			return sphere.Sample(origin, sample);
		}

		throw NotBasePackException();
	}

	/// <summary>
	/// Underlying implementation of <see cref="PreparedScene.ProbabilityDensity"/>, functional
	/// according to the local coordinate system of this <see cref="PreparedPack"/>.
	/// </summary>
	public float ProbabilityDensity(in NodeToken token, in Float3 origin, in Float3 incident)
	{
		Assert.IsTrue(token.IsGeometry);

		if (token.IsTriangle)
		{
			ref readonly var triangle = ref triangles[token.TriangleValue];
			return triangle.ProbabilityDensity(origin, incident);
		}

		if (token.IsSphere)
		{
			ref readonly var sphere = ref spheres[token.SphereValue];
			return sphere.ProbabilityDensity(origin, incident);
		}

		throw NotBasePackException();
	}

	/// <summary>
	/// Returns the cost of an intersection calculation between <paramref name="ray"/> and the object represented by <paramref name="token"/>.
	/// </summary>
	public int GetTraceCost(in Ray ray, ref float distance, in NodeToken token)
	{
		Assert.IsTrue(token.IsGeometry);

		if (token.IsTriangle)
		{
			ref readonly var triangle = ref triangles[token.TriangleValue];
			distance = Math.Min(distance, triangle.Intersect(ray, out _));
			return 1;
		}

		if (token.IsSphere)
		{
			ref readonly var sphere = ref spheres[token.SphereValue];
			distance = Math.Min(distance, sphere.Intersect(ray, out _));
			return 1;
		}

		return instances[token.InstanceValue].TraceCost(ray, ref distance);
	}

	/// <summary>
	/// Creates a new <see cref="PreparedPack"/>; outputs the <paramref name="extractor"/> and
	/// <paramref name="tokens"/> that were used to construct this new <see cref="PreparedPack"/>.
	/// </summary>
	public static PreparedPack Create(ScenePreparer preparer, EntityPack pack, out SwatchExtractor extractor, out NodeTokenArray tokens)
	{
		//Collect objects
		var trianglesList = new ConcurrentList<PreparedTriangle>();
		var spheresList = new List<PreparedSphere>();
		var instancesList = new List<PreparedInstance>();

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
					NodeToken token = NodeToken.CreateInstance((uint)instancesList.Count);
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
		var spheres = new PreparedSphere[spheresList.Count];
		var instances = new PreparedInstance[instancesList.Count];

		//Collect tokens
		tokens = CreateTokenArray(extractor, instances.Length);
		var aabbs = new AxisAlignedBoundingBox[tokens.TotalLength];

		var tokenArray = tokens;

		Parallel.For(0, triangles.Length, FillTriangles);
		Parallel.For(0, spheres.Length, FillSpheres);
		Parallel.For(0, instances.Length, FillInstances);

		Assert.IsTrue(tokens.IsFull);

		//Construct instance
		return new PreparedPack(preparer.profile.AggregatorProfile, aabbs, tokens, triangles, spheres, instances);

		void FillTriangles(int index)
		{
			ref readonly var triangle = ref trianglesList[index];

			triangles[index] = triangle;

			var token = NodeToken.CreateTriangle((uint)index);
			int at = tokenArray.Add(triangle.material, token);

			aabbs[at] = triangle.AABB;
		}

		void FillSpheres(int index)
		{
			var sphere = spheres[index] = spheresList[index];

			var token = NodeToken.CreateSphere((uint)index);
			int at = tokenArray.Add(sphere.material, token);

			aabbs[at] = sphere.AABB;
		}

		void FillInstances(int index)
		{
			PreparedInstance instance = instances[index] = instancesList[index];
			int at = tokenArray.Add(tokenArray.FinalPartition, instance.token);
			Assert.AreEqual((uint)index, instance.token.InstanceValue);

			aabbs[at] = instance.AABB;
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

	static NodeTokenArray CreateTokenArray(SwatchExtractor extractor, int instanceCount)
	{
		bool hasInstance = instanceCount > 0;

		ReadOnlySpan<MaterialIndex> indices = extractor.Indices;
		int totalLength = indices.Length + (hasInstance ? 1 : 0);

		Span<int> lengths = stackalloc int[totalLength];
		if (hasInstance) lengths[^1] = instanceCount;

		foreach (MaterialIndex index in indices) lengths[index] = extractor.GetRegistrationCount(index);

		return new NodeTokenArray(lengths);
	}

	/// <summary>
	/// Handles tokens of <see cref="PreparedInstance"/>, which is invalid since tokens should be resolved to pure geometry.
	/// </summary>
	static Exception NotBasePackException([CallerMemberName] string name = default) => new($"{name} should be invoked on the base {nameof(PreparedPack)}, not one with a token that is a {nameof(NodeToken.IsInstance)}!");
}