using System;
using System.Buffers;

namespace Echo.Common.Memory;

/// <summary>
/// A generic memory pool.
/// </summary>
public static class Pool<T>
{
	static ArrayPool<T> Internal => ArrayPool<T>.Shared;

	/// <summary>
	/// Receives and outputs a pooled <paramref name="view"/> with <paramref name="length"/>, which should be
	/// promptly caught via 'using statements' on the returned <see cref="ReleaseHandle"/> of this method.
	/// NOTE: the output <paramref name="view"/> is not guaranteed to be all empty!
	/// </summary>
	public static ReleaseHandle Fetch(int length, out View<T> view)
	{
		T[] array = Internal.Rent(length);
		view = array.AsView(0, length);
		return new ReleaseHandle(array);
	}

	/// <summary>
	/// Receives and outputs a pooled <paramref name="view"/> with <paramref name="length"/>, which
	/// should be promptly returned via 'using statements' on the returned result of this method.
	/// NOTE: the output <paramref name="view"/> is all empty.
	/// </summary>
	public static ReleaseHandle FetchClean(int length, out View<T> view)
	{
		var handle = Fetch(length, out view);
		view.AsSpan().Clear();
		return handle;
	}

	public struct ReleaseHandle : IDisposable
	{
		public ReleaseHandle(T[] array) => this.array = array;

		T[] array;

		void IDisposable.Dispose()
		{
			if (array == null) return;
			Internal.Return(array);
			array = null;
		}
	}
}