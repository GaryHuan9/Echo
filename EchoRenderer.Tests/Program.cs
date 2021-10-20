using System;
using BenchmarkDotNet.Running;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Tests
{
	public class Program
	{
		static void Main()
		{
			// BenchmarkRunner.Run<TestSIMD>();
			// BenchmarkRunner.Run<BenchmarkBVH>();
			// BenchmarkRunner.Run<BenchmarkTexture>();
			// BenchmarkRunner.Run<BenchmarkRadixSort>();
			// BenchmarkRunner.Run<BenchmarkLoop>();
			// BenchmarkRunner.Run<AxisAlignedBoundingBoxSIMD>();
			BenchmarkRunner.Run<BenchmarkEquals>();
		}
	}
}