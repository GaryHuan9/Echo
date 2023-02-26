using System;
using System.Diagnostics;
using System.Threading;

namespace Echo.Core.Common.Diagnostics;

/// <summary>
/// A mutable struct to help define and ensure that a set of operations must invoke on the same <see cref="Thread"/>.
/// </summary>
public struct MonoThread
{

#if DEBUG
	internal long threadId;
#endif

}

public static class MonoThreadExtensions
{
	//NOTE: We use extension methods rather than instance methods to use `ref this` to avoid side effects of mutable structs

	/// <summary>
	/// Ensures that the calling <see cref="Thread"/> is the same as all previous calling <see cref="Thread"/>s to this method.
	/// When first time invoking on this <paramref name="monoThread"/>, <see cref="Environment.CurrentManagedThreadId"/> is recorded.
	/// </summary>
	[Conditional("DEBUG")]
	public static void Ensure(ref this MonoThread monoThread)
	{
#if DEBUG
		ref long reference = ref monoThread.threadId;

		long threadId = (uint)Environment.CurrentManagedThreadId + 1L;
		long original = Interlocked.Exchange(ref reference, threadId);

		if (original == default || original == threadId) return;

		throw new InvalidOperationException($"Multiple threads (id {ToId(original)} and {ToId(threadId)}) attempting to use {nameof(MonoThread)}!");

		static int ToId(long value) => (int)(value - 1);
#endif
	}

	/// <summary>
	/// Resets <paramref name="monoThread"/> as so if it was new to accept a different thread.
	/// </summary>
	[Conditional("DEBUG")]
	public static void Reset(ref this MonoThread monoThread)
	{
#if DEBUG
		monoThread.threadId = default;
#endif
	}
}