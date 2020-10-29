using ForceRenderer.Objects;

namespace ForceRenderer.Modifiers
{
	public abstract class Modifier : SceneObject
	{
		protected Modifier(SceneObject encapsulated) => this.encapsulated = encapsulated;

		public readonly SceneObject encapsulated;
	}
}