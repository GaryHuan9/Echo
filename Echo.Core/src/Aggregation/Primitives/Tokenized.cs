namespace Echo.Core.Aggregation.Primitives;

public readonly record struct Tokenized<T>(EntityToken token, in T content) where T : unmanaged
{
	public readonly EntityToken token = token;
	public readonly T content = content;
}