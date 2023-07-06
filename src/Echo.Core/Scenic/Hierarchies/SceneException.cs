using System;

namespace Echo.Core.Scenic.Hierarchies;

/// <summary>
/// An <see cref="Exception"/> thrown when a <see cref="Scene"/> related error occured.
/// </summary>
[Serializable]
public class SceneException : Exception
{
	public SceneException() { }
	public SceneException(string message) : base(message) { }

	public static SceneException ModifiedTransform(string name) => new($"Unable to modify this specific transform component on '{name}'.");
	public static SceneException RootNotScene(string name) => new($"Cannot add '{name}' to an {nameof(EntityPack)} that is not a {nameof(Scene)}.");
	public static SceneException AmbiguousTransform(Entity entity) => new($"Unable to interpret the exact transform value of '{entity}'.");
}