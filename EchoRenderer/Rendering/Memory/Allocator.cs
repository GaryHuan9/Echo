using System;
using System.Threading;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;

namespace EchoRenderer.Rendering.Memory
{
	public class Allocator
	{
		public Allocator(int poolerCapacity = 8)
		{
			this.poolerCapacity = poolerCapacity;
			lock (tokens) { } //Wait for preparation
		}

		/// <summary>
		/// The number of pooler and pointers actually being utilized right now.
		/// </summary>
		int utilization;

		readonly int poolerCapacity;
		static int tokenCount;

		object[] poolers = new object[InitialSize];
		ushort[] pointers = new ushort[InitialSize];

		static ConcurrentList<Token> tokens = new();

		const int InitialSize = 8;

		/// <summary>
		/// Allocates new object of type <typeparamref name="T"/>.
		/// </summary>
		public T New<T>() where T : class, new()
		{
			int token = TokenStorage<T>.Fetch();
			int count = token + 1;

			EnsureCapacity(count);
			utilization = Math.Max(utilization, count);

			ref object pooler = ref poolers[token];
			ref ushort pointer = ref pointers[token];

			pooler ??= new T[poolerCapacity];

			ref T target = ref ((T[])pooler)[pointer++];
			return target ??= new T();
		}

		/// <summary>
		/// Releases all allocated objects and prepares for new allocations.
		/// </summary>
		public void Release() => Array.Clear(pointers, 0, utilization);

		void EnsureCapacity(int capacity)
		{
			object[] oldPoolers = poolers;
			ushort[] oldPointers = pointers;

			int length = oldPoolers.Length;
			if (length >= capacity) return;

			while (length < capacity) length *= 2;

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
		/// Clear the internal static state of <see cref="Allocator"/> to prepare
		/// for the creation of a new batch of <see cref="Allocator"/>.
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

		static class TokenStorage<T> where T : class, new()
		{
			static readonly Token token = new();

			public static int Fetch()
			{
				//Simply read token if already assigned
				ref int read = ref token.value;
				int original = Volatile.Read(ref read);
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
}