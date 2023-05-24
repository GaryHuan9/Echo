using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;
using Echo.Core.Common.Memory;

namespace Echo.Experimental.Benchmarks;

public class BinarySearches
{
	[GlobalSetup]
	public void Setup()
	{
		data = new AlignedArray<float>(size);

		float value = Random.Shared.NextSingle();
		
		for (int i = 0; i < size; i++)
		{
			data[i] = i * value;
		}

		targetIndex = Random.Shared.Next(size);
		targetValue = data[targetIndex];
	}

	[Params(1000)]//, 10000, 100000, 1000000)]
	public int size;

	AlignedArray<float> data;

	float targetValue;
	int targetIndex;
	
	[Benchmark]
	public int BinarySearchScalar()
	{
		uint head = 0u;
		uint tail = (uint)size;
		ref float origin = ref data[0];

		while (head < tail)
		{
			uint index = (tail + head) >> 1;

			var current = Unsafe.Add(ref origin, index);
			if (current == targetValue) return (int)index;

			if (current > targetValue) tail = index;
			else head = index + 1u;
		}

		return (int)~head;
	}

	[Benchmark]
	public unsafe int BinarySearchVectorial()
	{
		Vector128<float> targetVector = Vector128.Create(targetValue);
		
		int simdWords = size / 4;
		int left = 0;
		int right = simdWords - 1;

		while (left <= right)
		{
			int mid = left + (right - left) / 2;
			Vector128<float> separators = Sse.LoadAlignedVector128(data.Pointer + mid * 4);
			int eqCompResult = Sse.MoveMask(Sse.CompareEqual(separators, targetVector));
			if (eqCompResult != 0)
			{
				int i = 0;
				for(int j = 3; j >= 0; j--)
				{
					int bitValue = (eqCompResult >> j) & 1;
					if (bitValue == 1)
					{
						int foundIndex =  mid * 4 + (3 - j);
						if (foundIndex != targetIndex) throw new Exception($"BinarySearchVectorial: TargetIndex({targetIndex}) != FoundIndex({foundIndex})");
						return foundIndex;
					}
				}
			}

			int mask = Sse.MoveMask(Sse.CompareGreaterThan(targetVector, separators));
			if (mask == 0x0f) right = mid - 1;		// all separators are larger than the search key
			else if (mask == 0x00) left = mid + 1;	// all separators are smaller than the search key
			else return size;						// search key is not in the list
		}
		
		return size;
	}
}