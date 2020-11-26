using System.Collections.Generic;
using CodeHelpers;
using ForceRenderer.Renderers;

namespace ForceRenderer.Objects.SceneObjects
{
	public abstract class SceneObject : Object
	{
		protected SceneObject(Material material) => Material = material;

		Material _material;

		public Material Material
		{
			get => _material;
			set => _material = value ?? throw ExceptionHelper.Invalid(nameof(value), InvalidType.isNull);
		}

		/// <summary>
		/// Enumerates through all of the triangles that can be extracted from this <see cref="SceneObject"/>.
		/// NOTE: Can simply return nothing if this object does not have any triangle.
		/// </summary>
		public abstract IEnumerable<PressedTriangle> ExtractTriangles(int materialToken);

		/// <summary>
		/// Enumerates through all of the spheres that can be extracted from this <see cref="SceneObject"/>.
		/// NOTE: Can simply return nothing if this object does not have any sphere.
		/// </summary>
		public abstract IEnumerable<PressedSphere> ExtractSpheres(int materialToken);
	}
}