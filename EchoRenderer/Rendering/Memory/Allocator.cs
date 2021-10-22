using System;

namespace EchoRenderer.Rendering.Memory
{
	public class Allocator
	{
		public Allocator(int capacity) => this.capacity = capacity;

		int count;

		readonly int capacity;

		object[] poolers  = new object[InitialSize];
		ushort[] pointers = new ushort[InitialSize];

		const int InitialSize = 8;

		public T New<T>() where T : class, new()
		{
			ref int token = ref TokenStorage<T>.token;

			if (token < 0)
			{
				if (count == poolers.Length) Double();
				token = count++;
			}

			ref object pooler = ref poolers[token];
			ref ushort pointer = ref pointers[token];

			pooler ??= new T[capacity];

			ref T target = ref ((T[])pooler)[pointer++];
			return target ??= new T();
		}

		public void Clear() => Array.Clear(pointers, 0, count);

		void Double()
		{
			object[] oldPoolers = poolers;
			ushort[] oldPointers = pointers;

			int length = oldPoolers.Length;

			poolers = new object[length * 2];
			pointers = new ushort[length * 2];

			for (int i = 0; i < count; i++)
			{
				poolers[i] = oldPoolers[i];
				pointers[i] = oldPointers[i];
			}
		}

		static class TokenStorage<T> where T : class, new()
		{
			public static int token = -1;
		}
	}
}