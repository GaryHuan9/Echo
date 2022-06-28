using System;
using System.Collections.Immutable;
using CodeHelpers.Diagnostics;
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
using Echo.Core.Scenic.Preparation;
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

	public View<Tokenized<LightBounds>> CreateBoundsView(PreparedSwatch emissiveSwatch)
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
				float power = geometry.GetPower(emissiveSwatch);

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
	public Probable<RGB128> Sample(EntityToken token, in GeometryPoint point, Sample2D sample, out Float3 incident, out float travel) { }

	/// <inheritdoc cref="IPreparedAreaLight.ProbabilityDensity"/>
	public float ProbabilityDensity(EntityToken token, in GeometryPoint point, in Float3 incident)
	{
		switch (token.LightType)
		{
			case LightType.Point: break;
			default:              throw new ArgumentOutOfRangeException();
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