using System;
using System.Threading;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;

namespace EchoRenderer.Common.Memory;

/// <summary>
/// A fast memory allocator that can be used to create any class with a default constructor.
/// This is essentially a heap memory pooler that reuses old objects, and thus newly created
/// objects have undefined state. Remember to explicitly invoke "reset" methods for them.
/// NOTE: All the static methods are thread safe, while the instance methods are unsafe.
/// </summary>
public class Allocator
{
	/// <param name="capacity">The maximum number of objects of the same type that the internal <see cref="poolers"/> can store.</param>
	public Allocator(int capacity = 32)
	{
		this.capacity = capacity;
		lock (tokens) { } //Wait for token preparation
	}

	/// <summary>
	/// The number of pooler and pointers actually being utilized right now.
	/// </summary>
	int utilization;

	/// <summary>
	/// Whether we are currently allocating using this <see cref="Allocator"/>.
	/// </summary>
	bool allocating;

	readonly int capacity;
	static int tokenCount;

	object[] poolers = new object[InitialSize];
	ushort[] pointers = new ushort[InitialSize];

	MonoThread monoThread;

	static ConcurrentList<Token> tokens = new();

	const int InitialSize = 8;

	/// <summary>
	/// If a session is not currently underway, begins a new allocation session on this <see cref="Allocator"/> and returns a
	/// <see cref="ReleaseHandle"/> which should be used to <see cref="Release"/> this session. Otherwise an exception is thrown.
	/// </summary>
	public ReleaseHandle Begin()
	{
		monoThread.Ensure();
		if (allocating) throw new Exception($"Cannot {nameof(Begin)} this {nameof(Allocator)} when it is already {nameof(allocating)}!");

		allocating = true;
		return new ReleaseHandle(this);
	}

	/// <summary>
	/// Releases all allocated objects and prepares for a new allocation session to <see cref="Begin"/>.
	/// </summary>
	public void Release()
	{
		monoThread.Ensure();
		if (!allocating) return;

		allocating = false;
		Array.Clear(pointers, 0, utilization);
	}

	/// <summary>
	/// Invokes <see cref="Release"/> and then immediate invoke <see cref="Begin"/> and returns its <see cref="ReleaseHandle"/>.
	/// </summary>
	public ReleaseHandle Restart()
	{
		Release();
		return Begin();
	}

	/// <summary>
	/// Allocates new object of type <typeparamref name="T"/>.
	/// Note that the returned object will have undefined state.
	/// </summary>
	public T New<T>() where T : class, new()
	{
		monoThread.Ensure();
		if (!allocating) throw new Exception($"Cannot allocate without starting a session using {nameof(Begin)}!");

		//Find pool based on token
		int token = TokenStorage<T>.Fetch();
		int count = token + 1;

		EnsureCapacity(count);
		utilization = Math.Max(utilization, count);

		ref object pooler = ref poolers[token];
		ref ushort pointer = ref pointers[token];

		int index = pointer++;

		//Within capacity, use pooled object
		if (index < capacity)
		{
			pooler ??= new T[capacity];

			ref T target = ref ((T[])pooler)[index];
			return target ??= new T();
		}

		DebugHelper.LogWarning($"Exceeding {nameof(capacity)}: consider setting a larger {nameof(capacity)} or expect a performance degradation!");
		return new T();
	}

	void EnsureCapacity(int newCapacity)
	{
		object[] oldPoolers = poolers;
		ushort[] oldPointers = pointers;

		int length = oldPoolers.Length;
		if (length >= newCapacity) return;

		while (length < newCapacity) length *= 2;

		poolers = new object[length];
		pointers = new ushort[length];

		//Because our arrays here should always be pretty small (< 128),
		//an explicit loop should be faster than any utility methods
		for (int i = 0; i < utilization; i++)
		{
			poolers[i] = oldPoolers[i];
			pointers[i] = oldPointers[i];
		}
	}

	/// <summary>
	/// Clear the internal static state of <see cref="Allocator"/> to prepare for the creation of a new batch of <see cref="Allocator"/>.
	/// </summary>
	public static void Clear()
	{
		lock (tokens)
		{
			Assert.IsFalse(tokens.Adding);
			foreach (var token in tokens) token.value = Token.Unmanaged;

			Interlocked.Exchange(ref tokenCount, 0);
			tokens = new ConcurrentList<Token>();
		}
	}

	public struct ReleaseHandle : IDisposable
	{
		public ReleaseHandle(Allocator allocator)
		{
			this.allocator = allocator;
			allocator.allocating = true;
		}

		Allocator allocator;

		public void Dispose()
		{
			allocator?.Release();
			allocator = null;
		}
	}

	/// <summary>
	/// Internally we use a field inside this generic static class to access the token that represents a generic type.
	/// Note that this is incredibly fast compare to any kind of lookups since everything is untangled by the compiler.
	/// </summary>
	// ReSharper disable once UnusedTypeParameter
	static class TokenStorage<T> where T : class, new()
	{
		// ReSharper disable once StaticMemberInGenericType
		static readonly Token token = new();

		public static int Fetch()
		{
			//Simply read token if already assigned
			ref int read = ref token.value;
			int original = Volatile.Read(ref read); //TODO: is this volatile read necessary?
			if (original >= 0) return original;

			//Attempt to reserve a spot for new token creation
			lock (token)
			{
				original = Volatile.Read(ref read);
				if (original >= 0) return original;

				//We successfully reserved a spot
				int value = Interlocked.Increment(ref tokenCount) - 1;
				original = Interlocked.Exchange(ref read, value);

				Assert.IsTrue(original < 0);
				tokens.ImmediateAdd(token);
				return value;
			}
		}
	}

	class Token
	{
		public int value = Unmanaged;

		public const int Unmanaged = -1;
	}
}