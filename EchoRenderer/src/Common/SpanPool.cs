using System;
using System.Buffers;

namespace EchoRenderer.Common;

/// <summary>
/// A utility static class that wraps <see cref="ArrayPool{T}.Shared"/>.
/// </summary>
public static class SpanPool<T>
{
	static ArrayPool<T> Pool => ArrayPool<T>.Shared;

	/// <summary>
	/// Receives and outputs a pooled <paramref name="span"/> with <paramref name="length"/>, which
	/// should be promptly returned via 'using statements' on the returned result of this method.
	/// NOTE: the output <paramref name="span"/> is not guaranteed to be all empty!
	/// </summary>
	public static ReleaseHandle Fetch(int length, out Span<T> span)
	{
		T[] array = Pool.Rent(length);
		span = array.AsSpan(0, length);
		return new ReleaseHandle(array);
	}

	/// <summary>
	/// Receives and outputs a pooled <paramref name="span"/> with <paramref name="length"/>, which
	/// should be promptly returned via 'using statements' on the returned result of this method.
	/// NOTE: the output <paramref name="span"/> is all empty.
	/// </summary>
	public static ReleaseHandle FetchClean(int length, out Span<T> span)
	{
		var handle = Fetch(length, out span);
		span.Clear();
		return handle;
	}

	public struct ReleaseHandle : IDisposable
	{
		public ReleaseHandle(T[] array) => this.array = array;

		T[] array;

		void IDisposable.Dispose()
		{
			if (array == null) return;
			Pool.Return(array);
			array = null;
		}
	}
}