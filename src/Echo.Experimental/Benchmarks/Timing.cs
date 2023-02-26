using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;

namespace Echo.Experimental.Benchmarks;

public class Timing
{
	// |             Method |         Mean |     Error |    StdDev |
	// |------------------- |-------------:|----------:|----------:|
	// |            Elapsed |    22.543 ns | 0.1086 ns | 0.1016 ns |
	// |                Now |   100.230 ns | 1.3172 ns | 1.2321 ns |
	// |             UtcNow |    27.267 ns | 0.0649 ns | 0.0607 ns |
	// |        TickCount64 |     1.605 ns | 0.0049 ns | 0.0041 ns |
	// | TotalProcessorTime | 1,317.933 ns | 6.0263 ns | 5.6370 ns |

	readonly Stopwatch stopwatch = Stopwatch.StartNew();

	[Benchmark]
	public TimeSpan Elapsed() => stopwatch.Elapsed;

	[Benchmark]
	public DateTime Now() => DateTime.Now;

	[Benchmark]
	public DateTime UtcNow() => DateTime.UtcNow;

	[Benchmark]
	public long TickCount64() => Environment.TickCount64;

	[Benchmark]
	public TimeSpan TotalProcessorTime() => Process.GetCurrentProcess().TotalProcessorTime;
}