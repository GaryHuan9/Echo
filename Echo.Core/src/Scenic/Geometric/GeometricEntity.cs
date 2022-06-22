using System.Collections.Generic;
using CodeHelpers;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Scenic.Geometric;

public interface IGeometricEntity<out T>
{
	/// <summary>
	/// The exact number of prepared geometry the <see cref="Extract"/> will yield. 
	/// </summary>
	uint Count { get; }

	/// <summary>
	/// Extracts all of the prepared geometry of type <typeparamref name="T"/> from this <see cref="IGeometricEntity{T}"/>.
	/// </summary>
	/// <param name="extractor">The <see cref="SwatchExtractor"/> use to extract the
	/// <see cref="MaterialIndex"/> from the corresponding <see cref="Material"/>.</param>
	/// <returns>An <see cref="IEnumerable{T}"/> that enables enumeration through the extracted values.</returns>
	IEnumerable<T> Extract(SwatchExtractor extractor);
}

public abstract class GeometricEntity : Entity
{
	NotNull<Material> _material = Invisible.instance;

	/// <summary>
	/// The <see cref="Echo.Core.Evaluation.Materials.Material"/> applied to this <see cref="GeometricEntity"/>.
	/// </summary>
	/// <remarks>This value must not be null.</remarks>
	public Material Material
	{
		get => _material;
		set => _material = value;
	}
}