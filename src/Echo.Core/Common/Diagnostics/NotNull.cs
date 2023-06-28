using System;

namespace Echo.Core.Common.Diagnostics;

/// <summary>
/// A simple struct wrapper that helps making a property not null.
/// </summary>
public readonly struct NotNull<T> where T : class
{
	public NotNull(T storage) => this.storage = storage ?? throw new NullReferenceException();

	readonly T storage;

	public T Value => storage ?? throw new NullReferenceException();

	public static implicit operator NotNull<T>(T target) => new(target);
	public static implicit operator T(NotNull<T> target) => target.Value;
}