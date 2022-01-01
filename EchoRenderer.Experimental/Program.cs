using BenchmarkDotNet.Running;

namespace EchoRenderer.Experimental
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
			BenchmarkRunner.Run<AxisAlignedBoundingBoxSIMD>();
			// BenchmarkRunner.Run<BenchmarkMath>();
		}
	}
}