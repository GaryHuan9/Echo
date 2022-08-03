using Echo.Core.Scenic.Hierarchies;

namespace Echo.Core.Scenic.Geometries;

/// <summary>
/// A source of triangles. Can be used to repeatedly fetch an <see cref="ITriangleStream"/> many times.
/// </summary>
/// <remarks>This is a factory interface for <see cref="ITriangleStream"/>. Can be implemented to feed a
/// custom stream of triangles into the <see cref="Scene"/> through a <see cref="MeshEntity"/>.</remarks>
public interface ITriangleSource
{
	/// <summary>
	/// Creates a new <see cref="ITriangleStream"/>.
	/// </summary>
	/// <returns>The newly created <see cref="ITriangleStream"/>.</returns>
	ITriangleStream CreateStream();
}