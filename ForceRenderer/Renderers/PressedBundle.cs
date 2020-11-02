using System;
using CodeHelpers.Vectors;
using ForceRenderer.Objects;
using ForceRenderer.Objects.SceneObjects;

namespace ForceRenderer.Renderers
{
	/// <summary>
	/// Large struct, all access should be made through references
	/// </summary>
	public readonly struct PressedBundle
	{
		public PressedBundle(int token, SceneObject sceneObject)
		{
			this.token = token;
			this.sceneObject = sceneObject;

			transformation = sceneObject.Transformation;
			material = sceneObject.Material.Pressed;
		}

		public readonly int token;
		public readonly SceneObject sceneObject;

		public readonly Transformation transformation;
		public readonly PressedMaterial material;
	}
}