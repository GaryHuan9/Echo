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
	}
}