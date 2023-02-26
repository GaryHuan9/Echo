namespace Echo.Core.Aggregation.Primitives;

public readonly record struct Tokenized<T>(EntityToken token, in T content) where T : unmanaged
{
	public readonly EntityToken token = token;
	public readonly T content = content;

	/// <summary>
	/// Converts a tuple with (<see cref="content"/>, <see cref="content"/>) to a matching <see cref="Tokenized{T}"/>.
	/// </summary>
	public static implicit operator Tokenized<T>(in (EntityToken token, T content) pair) => new(pair.token, pair.content);
}