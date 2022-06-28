using System.Collections.Generic;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Scenic.Geometric;

public interface IGeometrySource { }

public interface IGeometrySource<out T> : IGeometrySource where T : IPreparedPureGeometry
{
	/// <summary>
	/// The exact number of prepared geometry the <see cref="Extract"/> will yield. 
	/// </summary>
	uint Count { get; }

	/// <summary>
	/// Extracts all of the prepared geometry of type <typeparamref name="T"/> from this <see cref="IGeometrySource{T}"/>.
	/// </summary>
	/// <param name="extractor">The <see cref="SwatchExtractor"/> use to extract the
	/// <see cref="MaterialIndex"/> from the corresponding <see cref="Material"/>.</param>
	/// <returns>An <see cref="IEnumerable{T}"/> that enables enumeration through the extracted values.</returns>
	IEnumerable<T> Extract(SwatchExtractor extractor);
}