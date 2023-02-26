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
}