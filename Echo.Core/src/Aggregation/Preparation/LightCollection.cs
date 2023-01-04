using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Scenic.Geometries;
using Echo.Core.Scenic.Lights;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Aggregation.Preparation;

/// <summary>
/// A collection that contains all of the light objects in a <see cref="PreparedScene"/>.
/// </summary>
public sealed class LightCollection
{
	public LightCollection(ReadOnlySpan<ILightSource> lightSources, GeometryCollection geometries)
	{
		points = Extract<PreparedPointLight>(lightSources);

		this.geometries = geometries;

		static ImmutableArray<T> Extract<T>(ReadOnlySpan<ILightSource> lightSources) where T : IPreparedLight
		{
			int length = 0;

			foreach (ILightSource source in lightSources)
			{
				if (source is ILightSource<T>) ++length;
			}

			var builder = ImmutableArray.CreateBuilder<T>(length);

			foreach (ILightSource source in lightSources)
			{
				if (source is ILightSource<T> match) builder.Add(match.Extract());
			}

			return builder.MoveToImmutable();
		}
	}

	public readonly ImmutableArray<PreparedPointLight> points;

	public readonly GeometryCollection geometries;

	/// <summary>
	/// A multiplier applied to the travel distance returned by <see cref="Sample"/> to
	/// avoid the <see cref="OccludeQuery"/> intersecting with the actual light geometry.
	/// </summary>
	const float TravelMultiplier = 1f - 2E-5f;

	public View<Tokenized<LightBound>> CreateBounds()
	{
		var list = new SmallList<Tokenized<LightBound>>();

		list.Expand(points.Length);

		AddLight(LightType.Point, points);

		AddEmissive(TokenType.Triangle, geometries.triangles);
		AddEmissive(TokenType.Sphere, geometries.spheres);

		for (int i = 0; i < geometries.instances.Length; i++)
		{
			ref readonly PreparedInstance instance = ref geometries.instances.ItemRef(i);

			if (!FastMath.Positive(instance.Power)) continue;
			var token = new EntityToken(TokenType.Instance, i);
			list.Add((token, instance.LightBound));
		}

		return list;

		void AddLight<T>(LightType type, ImmutableArray<T> array) where T : IPreparedLight
		{
			for (int i = 0; i < array.Length; i++)
			{
				var token = new EntityToken(type, i);
				list.Add((token, array[i].LightBound));
			}
		}

		void AddEmissive<T>(TokenType type, ImmutableArray<T> array) where T : IPreparedGeometry
		{
			for (int i = 0; i < array.Length; i++)
			{
				ref readonly T geometry = ref array.ItemRef(i);

				if (geometries.swatch[geometry.Material] is not Emissive emissive) continue;

				float power = emissive.Power * geometry.Area;
				if (!FastMath.Positive(power)) continue;

				list.Add
				((
					new EntityToken(type, i),
					new LightBound(geometry.BoxBound, geometry.ConeBound, power)
				));
			}
		}
	}

	/// <inheritdoc cref="IPreparedLight.Sample"/>
	public Probable<RGB128> Sample(EntityToken token, in GeometryPoint origin, Sample2D sample, out Float3 incident, out float travel)
	{
		switch (token.Type)
		{
			case TokenType.Triangle:
			{
				ref readonly PreparedTriangle triangle = ref geometries.triangles.ItemRef(token.Index);
				if (geometries.swatch[triangle.Material] is not Emissive emissive) return Exit(out incident, out travel);
				return HandleGeometry(triangle, emissive, origin, sample, out incident, out travel);
			}
			case TokenType.Sphere:
			{
				ref readonly PreparedSphere sphere = ref geometries.spheres.ItemRef(token.Index);
				if (geometries.swatch[sphere.Material] is not Emissive emissive) return Exit(out incident, out travel);
				return HandleGeometry(sphere, emissive, origin, sample, out incident, out travel);
			}
			case TokenType.Light:
			{
				switch (token.LightType)
				{
					case LightType.Point: return points[token.LightIndex].Sample(origin, sample, out incident, out travel);
					default:              throw new ArgumentOutOfRangeException(nameof(token));
				}
			}
			default: throw new ArgumentOutOfRangeException(nameof(token));
		}

		static Probable<RGB128> HandleGeometry<T>(in T geometry, Emissive material, in GeometryPoint origin,
												  Sample2D sample, out Float3 incident, out float travel) where T : IPreparedGeometry
		{
			(GeometryPoint point, float pdf) = geometry.Sample(origin, sample);
			if (!FastMath.Positive(pdf)) return Exit(out incident, out travel);

			Float3 delta = point.position - origin;
			float travel2 = delta.SquaredMagnitude;

			if (!FastMath.Positive(travel2)) return Exit(out incident, out travel);

			travel = FastMath.Sqrt0(travel2);
			incident = delta * (1f / travel);

			travel *= TravelMultiplier;
			return (material.Emit(point, -incident), pdf);
		}

		[SkipLocalsInit]
		static Probable<RGB128> Exit(out Float3 incident, out float travel)
		{
			Unsafe.SkipInit(out incident);
			Unsafe.SkipInit(out travel);
			return Probable<RGB128>.Impossible;
		}
	}

	/// <inheritdoc cref="IPreparedLight.ProbabilityDensity"/>
	public float ProbabilityDensity(EntityToken token, in GeometryPoint origin, in Float3 incident)
	{
		switch (token.Type)
		{
			case TokenType.Triangle: return HandleGeometry(geometries.triangles, token, origin, incident);
			case TokenType.Sphere:   return HandleGeometry(geometries.spheres, token, origin, incident);
			case TokenType.Light:
			{
				switch (token.LightType)
				{
					case LightType.Point: return 1f;
					default:              throw new ArgumentOutOfRangeException(nameof(token));
				}
			}
			default: throw new ArgumentOutOfRangeException(nameof(token));
		}

		static float HandleGeometry<T>(ImmutableArray<T> array, EntityToken token,
									   in GeometryPoint origin, in Float3 incident) where T : IPreparedGeometry
		{
			ref readonly T geometry = ref array.ItemRef(token.Index);
			return geometry.ProbabilityDensity(origin, incident);
		}
	}

	struct SmallList<T>
	{
		public SmallList()
		{
			array = Array.Empty<T>();
			length = 0;
		}

		T[] array;
		int length;

		public void Add(in T item)
		{
			if (array.Length <= length) Expand(1);
			array[length++] = item;
		}

		public void Expand(int count) => Utility.EnsureCapacity(ref array, length + count, true);

		public static implicit operator View<T>(SmallList<T> list) => list.array.AsView(0, list.length);
	}
}