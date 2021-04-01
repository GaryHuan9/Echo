using System;
using System.Collections.Generic;
using CodeHelpers;
using ForceRenderer.Rendering.Materials;

namespace ForceRenderer.Objects.GeometryObjects
{
	public abstract class GeometryObject : Object
	{
		protected GeometryObject(Material material) => Material = material;

		Material _material;

		public virtual Material Material
		{
			get => _material;
			set => _material = value ?? throw ExceptionHelper.Invalid(nameof(value), InvalidType.isNull);
		}

		/// <summary>
		/// Enumerates through all of the triangles that can be extracted from this <see cref="GeometryObject"/>.
		/// NOTE: Can simply return empty enumerable if this object does not have any triangle.
		/// </summary>
		public abstract IEnumerable<PressedTriangle> ExtractTriangles(Func<Material, int> materialConverter);

		/// <summary>
		/// Enumerates through all of the spheres that can be extracted from this <see cref="GeometryObject"/>.
		/// NOTE: Can simply return empty enumerable if this object does not have any sphere.
		/// </summary>
		public abstract IEnumerable<PressedSphere> ExtractSpheres(Func<Material, int> materialConverter);
	}
}