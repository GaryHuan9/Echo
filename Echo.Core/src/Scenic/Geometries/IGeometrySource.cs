using System.Collections.Generic;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Scenic.Geometries;

/// <summary>
/// A non generic variant of <see cref="IGeometrySource{T}"/>; should not be directly implemented.
/// </summary>
public interface IGeometrySource { }

/// <summary>
/// Implemented by <see cref="MaterialEntity"/>s that are sources from which <see cref="IPreparedGeometry"/>s can be extracted.
/// </summary>
/// <typeparam name="T">The type of the <see cref="IPreparedGeometry"/>
/// that this <see cref="IGeometrySource{T}"/> can produce.</typeparam>
public interface IGeometrySource<out T> : IGeometrySource where T : IPreparedGeometry
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