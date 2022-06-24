using System.Collections.Immutable;
using Echo.Core.Common.Memory;
using Echo.Core.Scenic.Lighting;

namespace Echo.Core.Aggregation.Preparation;

public class LightCollection
{
	public LightCollection(ReadOnlyView<ILightSource> lightSources, GeometryCollection geometries)
	{
		points = Extract<PreparedPointLight>();

		this.geometries = geometries;

		ImmutableArray<T> Extract<T>()
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
}