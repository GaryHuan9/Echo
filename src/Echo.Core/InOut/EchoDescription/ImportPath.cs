namespace Echo.Core.InOut.EchoDescription;

/// <summary>
/// A special type so the <see cref="LiteralParser"/> knows a string represents a path.
/// </summary>
public readonly struct ImportPath
{
	public ImportPath(string path) => this.path = path;

	readonly string path;

	public static implicit operator string(ImportPath path) => path.path;
	public static implicit operator ImportPath(string path) => new(path);
}