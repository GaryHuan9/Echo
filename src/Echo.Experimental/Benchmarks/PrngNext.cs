using BenchmarkDotNet.Attributes;
using Echo.Core.Common.Mathematics.Randomization;

namespace Echo.Experimental.Benchmarks;

public class PrngNext
{
	[ParamsSource(nameof(Generators))]
	public Prng Prng { get; set; }

	public Prng[] Generators { get; } = { new SeedlessPrng(), new SystemPrng(42), new SquirrelPrng(42) };

	// |   Method |             Prng |       Mean |     Error |    StdDev |
	// |--------- |----------------- |-----------:|----------:|----------:|
	// |    Next1 | SeedlessPrng { } |  10.601 ns | 0.1096 ns | 0.1025 ns |
	// |    Next4 | SeedlessPrng { } |  41.445 ns | 0.4446 ns | 0.3713 ns |
	// | Next1Int | SeedlessPrng { } |   5.345 ns | 0.0613 ns | 0.0574 ns |
	// | Next4Int | SeedlessPrng { } |  24.960 ns | 0.1945 ns | 0.1820 ns |
	// |    Next1 | SquirrelPrng { } |   9.153 ns | 0.1079 ns | 0.1009 ns |
	// |    Next4 | SquirrelPrng { } |  40.646 ns | 0.2159 ns | 0.2019 ns |
	// | Next1Int | SquirrelPrng { } |   3.879 ns | 0.0339 ns | 0.0317 ns |
	// | Next4Int | SquirrelPrng { } |  20.182 ns | 0.4171 ns | 0.4803 ns |
	// |    Next1 |   SystemPrng { } |  50.452 ns | 0.2745 ns | 0.2568 ns |
	// |    Next4 |   SystemPrng { } | 199.309 ns | 1.5522 ns | 1.4519 ns |
	// | Next1Int |   SystemPrng { } |  24.736 ns | 0.1247 ns | 0.1167 ns |
	// | Next4Int |   SystemPrng { } | 104.627 ns | 0.8718 ns | 0.8155 ns |

	// After optimizing the common case for Next1
	// | Method |             Prng |       Mean |     Error |    StdDev |
	// |------- |----------------- |-----------:|----------:|----------:|
	// |  Next1 | SeedlessPrng { } |   6.041 ns | 0.0525 ns | 0.0491 ns |
	// |  Next4 | SeedlessPrng { } |  25.733 ns | 0.3797 ns | 0.3551 ns |
	// |  Next1 | SquirrelPrng { } |   4.235 ns | 0.0551 ns | 0.0515 ns |
	// |  Next4 | SquirrelPrng { } |  20.768 ns | 0.1597 ns | 0.1494 ns |
	// |  Next1 |   SystemPrng { } |  26.088 ns | 0.2020 ns | 0.1889 ns |
	// |  Next4 |   SystemPrng { } | 107.965 ns | 0.7225 ns | 0.6758 ns |

	[Benchmark]
	public void Next1() => Prng.Next1();

	[Benchmark]
	public void Next4() => Prng.Next4();

	[Benchmark]
	public void Next1Int() => Prng.Next1(1234567);

	[Benchmark]
	public void Next4Int() => Prng.Next4(1234567);

	//The seedless system prng uses a faster implementation
	record SeedlessPrng : Prng
	{
		readonly SystemPrng impl = new();

		public override uint NextUInt32() => impl.NextUInt32();
	}
}