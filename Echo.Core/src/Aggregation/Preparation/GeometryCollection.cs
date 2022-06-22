using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Geometric;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Aggregation.Preparation;

public class GeometryCollection
{
	public GeometryCollection(AggregatorProfile profile, PreparedTriangle[] triangles, PreparedInstance[] instances, PreparedSphere[] spheres)
	{
		counts = new GeometryCounts(triangles.Length, instances.Length, spheres.Length);

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

		aggregator = profile.CreateAggregator(this, aabbs, tokens);

		this.triangles = triangles;
		this.instances = instances;
		this.spheres = spheres;
	}

	public readonly Aggregator aggregator;
	public readonly GeometryCounts counts;

	readonly PreparedTriangle[] triangles;
	readonly PreparedInstance[] instances;
	readonly PreparedSphere[] spheres;

	/// <summary>
	/// Calculates the intersection between <paramref name="query"/> and the object represented by <paramref name="token"/>.
	/// The intersection is only recorded if it occurs before the original <paramref name="query.distance"/>.
	/// </summary>
	public void Trace(ref TraceQuery query, in EntityToken token)
	{
		Assert.IsTrue(token.Type.IsGeometry());

		switch (token.Type)
		{
			case TokenType.Triangle:
			{
				query.current.TopToken = token;
				if (query.ignore == query.current) return;

				ref readonly PreparedTriangle triangle = ref triangles[token.Index];
				float distance = triangle.Intersect(query.ray, out Float2 uv);

				if (distance >= query.distance) return;

				query.token = query.current;
				query.distance = distance;
				query.uv = uv;

				break;
			}
			case TokenType.Instance:
			{
				instances[token.Index].Trace(ref query);
				break;
			}
			case TokenType.Sphere:
			{
				query.current.TopToken = token;
				bool findFar = query.ignore == query.current;

				ref readonly PreparedSphere sphere = ref spheres[token.Index];
				float distance = sphere.Intersect(query.ray, out Float2 uv, findFar);

				if (distance >= query.distance) return;

				query.token = query.current;
				query.distance = distance;
				query.uv = uv;

				break;
			}
			default: throw new ArgumentOutOfRangeException(nameof(token));
		}
	}

	/// <summary>
	/// Calculates and returns whether <paramref name="query"/> is occluded by the object represented by <paramref name="token"/>.
	/// </summary>
	public bool Occlude(ref OccludeQuery query, in EntityToken token)
	{
		Assert.IsTrue(token.Type.IsGeometry());

		switch (token.Type)
		{
			case TokenType.Triangle:
			{
				query.current.TopToken = token;
				if (query.ignore == query.current) return false;

				ref readonly var triangle = ref triangles[token.Index];
				return triangle.Intersect(query.ray, query.travel);
			}
			case TokenType.Instance:
			{
				return instances[token.Index].Occlude(ref query);
			}
			case TokenType.Sphere:
			{
				query.current.TopToken = token;
				bool findFar = query.ignore == query.current;

				ref readonly var sphere = ref spheres[token.Index];
				return sphere.Intersect(query.ray, query.travel, findFar);
			}
			default: throw new ArgumentOutOfRangeException(nameof(token));
		}
	}

	/// <summary>
	/// Creates a new <see cref="Touch"/> from <paramref name="query"/>. The <see cref="Material"/> is extracted from <paramref name="swatch"/>.
	/// The world to local <see cref="Float4x4"/> for the geometry found with <paramref name="query"/> should be passed through <paramref name="transform"/>.
	/// </summary>
	public Touch Interact(in TraceQuery query, PreparedSwatch swatch, in Float4x4 transform)
	{
		EntityToken token = query.token.TopToken;
		Assert.IsTrue(token.Type.IsRawGeometry());

		Float3 normal;
		Float2 texcoord;
		MaterialIndex materialIndex;

		switch (token.Type)
		{
			case TokenType.Triangle:
			{
				ref readonly var triangle = ref triangles[token.Index];

				normal = triangle.GetNormal(query.uv);
				texcoord = triangle.GetTexcoord(query.uv);
				materialIndex = triangle.material;

				break;
			}
			case TokenType.Sphere:
			{
				ref readonly var sphere = ref spheres[token.Index];

				normal = PreparedSphere.GetNormal(query.uv);
				texcoord = PreparedSphere.GetTexcoord(query.uv);
				materialIndex = sphere.material;

				break;
			}
			default: throw new ArgumentOutOfRangeException(nameof(query));
		}

		normal = transform.MultiplyDirection(normal).Normalized; //Apply world transform to normal
		Material material = swatch[materialIndex];               //Find appropriate mapped material

		//Construct touch
		if (material == null) return new Touch(query, normal);
		return new Touch(query, normal, material, texcoord);
	}

	/// <summary>
	/// Returns the <see cref="PreparedInstance"/> stored in this <see cref="PreparedPack"/> represented by <paramref name="token"/>.
	/// </summary>
	public PreparedInstance GetInstance(in EntityToken token)
	{
		Assert.AreEqual(token.Type, TokenType.Instance);
		return instances[token.Index];
	}

	/// <summary>
	/// Returns the <see cref="MaterialIndex"/> of the geometry represented by <paramref name="token"/>.
	/// </summary>
	public MaterialIndex GetMaterialIndex(in EntityToken token)
	{
		Assert.IsTrue(token.Type.IsRawGeometry());

		return token.Type switch
		{
			TokenType.Triangle => triangles[token.Index].material,
			TokenType.Sphere   => spheres[token.Index].material,
			_                  => throw new ArgumentOutOfRangeException(nameof(token))
		};
	}

	/// <summary>
	/// Returns the area of the geometry represented by <paramref name="token"/>.
	/// </summary>
	public float GetArea(in EntityToken token)
	{
		Assert.IsTrue(token.Type.IsRawGeometry());

		return token.Type switch
		{
			TokenType.Triangle => triangles[token.Index].Area,
			TokenType.Sphere   => spheres[token.Index].Area,
			_                  => throw new ArgumentOutOfRangeException(nameof(token))
		};
	}

	/// <summary>
	/// Underlying implementation of <see cref="PreparedScene.Sample"/>, functional
	/// according to the local coordinate system of this <see cref="PreparedPack"/>.
	/// </summary>
	public Probable<GeometryPoint> Sample(in EntityToken token, in Float3 origin, Sample2D sample)
	{
		Assert.IsTrue(token.Type.IsRawGeometry());

		return token.Type switch
		{
			TokenType.Triangle => triangles[token.Index].Sample(origin, sample),
			TokenType.Sphere   => spheres[token.Index].Sample(origin, sample),
			_                  => throw new ArgumentOutOfRangeException(nameof(token))
		};
	}

	/// <summary>
	/// Underlying implementation of <see cref="PreparedScene.ProbabilityDensity"/>, functional
	/// according to the local coordinate system of this <see cref="PreparedPack"/>.
	/// </summary>
	public float ProbabilityDensity(in EntityToken token, in Float3 origin, in Float3 incident)
	{
		Assert.IsTrue(token.Type.IsRawGeometry());

		return token.Type switch
		{
			TokenType.Triangle => triangles[token.Index].ProbabilityDensity(origin, incident),
			TokenType.Sphere   => spheres[token.Index].ProbabilityDensity(origin, incident),
			_                  => throw new ArgumentOutOfRangeException(nameof(token))
		};
	}

	/// <summary>
	/// Returns the cost of an intersection calculation between <paramref name="ray"/> and the object represented by <paramref name="token"/>.
	/// </summary>
	public uint GetTraceCost(in Ray ray, ref float distance, in EntityToken token)
	{
		Assert.IsTrue(token.Type.IsGeometry());

		switch (token.Type)
		{
			case TokenType.Triangle:
			{
				ref readonly PreparedTriangle triangle = ref triangles[token.Index];
				distance = Math.Min(distance, triangle.Intersect(ray, out _));
				return 1;
			}
			case TokenType.Instance:
			{
				return instances[token.Index].TraceCost(ray, ref distance);
			}
			case TokenType.Sphere:
			{
				ref readonly PreparedSphere sphere = ref spheres[token.Index];
				distance = Math.Min(distance, sphere.Intersect(ray, out _));
				return 1;
			}
			default: throw new ArgumentOutOfRangeException(nameof(token));
		}
	}
}