using System;
using System.Collections.Immutable;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Scenic.Lighting;

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
		Tokenized<LightBounds>[] result;

		for (int i = 0; i < points.Length; i++)
		{
			var token = new EntityToken(LightType.Point, i);
			AxisAlignedBoundingBox bounds = points[i].AABB;

			fill.Add((token, bounds));
		}
		
		var fill = result.AsFill();

		for (int i = 0; i < triangles.Length; i++)
		{
			var token = new EntityToken(TokenType.Triangle, i);
			AxisAlignedBoundingBox bounds = triangles[i].AABB;

			fill.Add((token, bounds));
		}

		for (int i = 0; i < spheres.Length; i++)
		{
			var token = new EntityToken(TokenType.Sphere, i);
			AxisAlignedBoundingBox bounds = spheres[i].AABB;

			fill.Add((token, bounds));
		}

		for (int i = 0; i < instances.Length; i++)
		{
			var token = new EntityToken(TokenType.Instance, i);
			AxisAlignedBoundingBox bounds = instances[i].AABB;

			fill.Add((token, bounds));
		}

		return result;
	}
}