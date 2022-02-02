using System.Collections.Generic;
using CodeHelpers;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Scenic.Preparation;

namespace EchoRenderer.Scenic.GeometryObjects
{
	public abstract class GeometryObject : Object
	{
		protected GeometryObject(Material material) => Material = material;

		NotNull<Material> _material;

		public Material Material
		{
			get => _material;
			set => _material = value;
		}

		/// <summary>
		/// Returns all of the triangle that is necessary to represent this <see cref="GeometryObject"/>.
		/// Use <paramref name="extractor"/> to register and retrieve tokens for <see cref="Material"/>.
		/// </summary>
		public abstract IEnumerable<PreparedTriangle> ExtractTriangles(SwatchExtractor extractor);

		/// <summary>
		/// Returns all of the sphere that is necessary to represent this <see cref="GeometryObject"/>.
		/// Use <paramref name="extractor"/> to register and retrieve tokens for <see cref="Material"/>.
		/// </summary>
		public abstract IEnumerable<PreparedSphere> ExtractSpheres(SwatchExtractor extractor);
	}
}