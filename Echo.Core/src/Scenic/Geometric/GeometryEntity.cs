using System.Collections.Generic;
using System.Linq;
using CodeHelpers;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Scenic.Geometric;

public abstract class GeometryEntity : Entity
{
	NotNull<Material> _material = Invisible.instance;

	public Material Material
	{
		get => _material;
		set => _material = value;
	}

	/// <summary>
	/// Extracts all of the <see cref="PreparedTriangle"/> that is necessary to represent this <see cref="GeometryEntity"/>.
	/// </summary>
	/// <remarks>Use <paramref name="extractor"/> to register and retrieve the <see cref="MaterialIndex"/> for <see cref="Material"/>.</remarks>
	public virtual IEnumerable<PreparedTriangle> ExtractTriangles(SwatchExtractor extractor) => Enumerable.Empty<PreparedTriangle>();

	/// <summary>
	/// Extracts all of the <see cref="PreparedSphere"/> that is necessary to represent this <see cref="GeometryEntity"/>.
	/// </summary>
	/// <remarks>Use <paramref name="extractor"/> to register and retrieve the <see cref="MaterialIndex"/> for <see cref="Material"/>.</remarks>
	public virtual IEnumerable<PreparedSphere> ExtractSpheres(SwatchExtractor extractor) => Enumerable.Empty<PreparedSphere>();
}