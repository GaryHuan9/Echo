using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;

namespace Echo.Experimental.Benchmarks;

public class BinarySearches
{
	[GlobalSetup]
	public void Setup()
	{
		data = new float[size];

		float value = Random.Shared.NextSingle();
		
		for (int i = 0; i < size; i++)
		{
			data[i] = (float)i * value;
		}

		targetIndex = Random.Shared.Next(size);
		targetValue = data[targetIndex];
	}

	[Params(1000)]//, 10000, 100000, 1000000)]
	public int size;

	float[] data;

	float targetValue;
	int targetIndex;
	
	[Benchmark]
	public int ClassicScalar()
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
	public int SequentialSearchScalar()
	{
		for (int i = 0; i < size; i++)
		{
			if (targetValue == data[i])
			{
				if (i != targetIndex) throw new Exception("SquentialSearchScalar: TargetIndex != FoundIndex");
				return i;
			}
		}
		
		// just for the function to not generate an error while building
		return size;
	}

	[Benchmark]
	public unsafe int SequentialSearchVectorial()
	{
		Vector128<float> targetVector = Vector128.Create(targetValue);
		for (int i = 0; i < size; i += 4)
		{
			//Vector128<float> dataValues = Vector128.Create(data[i], data[i + 1], data[i + 2], data[i + 3]);
			fixed (float* dataAddr = &data[i])
			{
				Vector128<float> dataValues = Sse.LoadVector128(dataAddr);
				int result = Sse.MoveMask(Sse42.CompareGreaterThanOrEqual(dataValues, targetVector));
				if (result != 0)
				{
					int index = i;
					for (int j = 3; j >= 0; j--)
					{
						int compareResult = (result >> j) & 1;
						if (compareResult != 0)
						{
							index += j;
							if (index != targetIndex) throw new Exception($"SequentialSearchVectorial: TargetIndex ({targetIndex}) != FoundIndex ({index})");
							return index;
						}
					}

					
				}
			}
		}

		return size;
	}
}