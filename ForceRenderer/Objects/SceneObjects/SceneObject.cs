using System;
using CodeHelpers;
using CodeHelpers.Vectors;
using ForceRenderer.Mathematics;
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
		/// Returns the distance from a ray to its intersection with the object in local-space.
		/// NOTE: Positive infinity will be returned if the intersection does not exist.
		/// </summary>
		public abstract float GetRawIntersection(in Ray ray);

		/// <summary>
		/// Returns the raw normal of this object at local-space <paramref name="point"/>.
		/// NOTE: The returned normal vector should be normalized.
		/// </summary>
		public abstract Float3 GetRawNormal(Float3 point);

		/// <summary>
		/// Invoked when this scene object is pressed. You can use this method to
		/// store cached values to offload the calculation offline.
		/// </summary>
		public virtual void OnPressed() { }
	}
}