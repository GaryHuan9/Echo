using System;
using System.Collections.Generic;
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
		/// Presses the scene object and add it to either of the lists. The material token for the pressed object is
		/// provided through <paramref name="materialToken"/>.
		/// </summary>
		public abstract void Press(List<PressedTriangle> triangles, List<PressedSphere> spheres, int materialToken);
	}
}