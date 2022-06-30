using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Geometric;
using Echo.Core.Scenic.Lighting;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Aggregation.Preparation;

public class LightCollection
{
	public LightCollection(ReadOnlySpan<ILightSource> lightSources, GeometryCollection geometries)
	{
		points = Extract<PreparedPointLight>(lightSources);

		this.geometries = geometries;

		static ImmutableArray<T> Extract<T>(ReadOnlySpan<ILightSource> lightSources)
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

	readonly GeometryCollection geometries;

	public View<Tokenized<LightBounds>> CreateBoundsView()
	{
		var list = new SmallList<Tokenized<LightBounds>>();

		list.Expand(points.Length);

		for (int i = 0; i < points.Length; i++)
		{
			var token = new EntityToken(LightType.Point, i);
			LightBounds bounds = points[i].LightBounds;

			list.Add((token, bounds));
		}

		AddEmissive(TokenType.Triangle, geometries.triangles);
		AddEmissive(TokenType.Sphere, geometries.spheres);
		AddEmissive(TokenType.Instance, geometries.instances);

		return list;

		void AddEmissive<T>(TokenType type, ImmutableArray<T> array) where T : IPreparedGeometry
		{
			for (int i = 0; i < array.Length; i++)
			{
				ref readonly T geometry = ref array.ItemRef(i);
				float power = geometry.GetPower(geometries.swatch);

				if (!FastMath.Positive(power)) continue;

				list.Add
				((
					new EntityToken(type, i),
					new LightBounds(geometry.AABB, geometry.ConeBounds, power)
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
				if (geometries.swatch[triangle.Material] is not IEmissive emissive) return Exit(out incident, out travel);
				return HandleGeometry(triangle, emissive, origin, sample, out incident, out travel);
			}
			case TokenType.Sphere:
			{
				ref readonly PreparedSphere sphere = ref geometries.spheres.ItemRef(token.Index);
				if (geometries.swatch[sphere.Material] is not IEmissive emissive) return Exit(out incident, out travel);
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

		static Probable<RGB128> HandleGeometry<T>(in T geometry, IEmissive material, in GeometryPoint origin,
												  Sample2D sample, out Float3 incident, out float travel) where T : IPreparedPureGeometry
		{
			(GeometryPoint point, float pdf) = geometry.Sample(origin, sample);
			if (!FastMath.Positive(pdf)) return Exit(out incident, out travel);

			Float3 delta = point.position - origin;
			float travel2 = delta.SquaredMagnitude;

			if (!FastMath.Positive(travel2)) return Exit(out incident, out travel);

			travel = FastMath.Sqrt0(travel2);
			incident = delta * (1f / travel);

			travel = FastMath.Max0(travel - FastMath.Epsilon);
			return (material.Emit(origin, -incident), pdf);
		}

		[SkipLocalsInit]
		static Probable<RGB128> Exit(out Float3 incident, out float travel)
		{
			Unsafe.SkipInit(out incident);
			Unsafe.SkipInit(out travel);
			return Probable<RGB128>.Impossible;
		}
	}

	/// <inheritdoc cref="IPreparedAreaLight.ProbabilityDensity"/>
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
									   in GeometryPoint origin, in Float3 incident) where T : IPreparedPureGeometry
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