using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Running;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Common.Mathematics.Primitives;
using Echo.Common.Mathematics.Randomization;
using Echo.Core.Aggregation.Acceleration;
using Echo.Experimental.Benchmarks;
using JitBuddy;

namespace Echo.Experimental;

public class Program
{
	static void Main()
	{
		TestMonteCarlo();
		// TestJitter();
		// TestUnmanaged();

		// BenchmarkRunner.Run<PackedFloats>();
		// BenchmarkRunner.Run<Aggregators>();
		// BenchmarkRunner.Run<RadixSort>();
		// BenchmarkRunner.Run<Loops>();
		// BenchmarkRunner.Run<AabbSimd>();
		// BenchmarkRunner.Run<MathFunctions>();
	}

	static void TestMonteCarlo()
	{
		Prng random = new SquirrelPrng();

		const decimal Threshold = 0.001m;

		int count = 0;

		decimal mean = 0m;
		decimal squared = 0m;

		decimal variance;
		decimal noise;

		do
		{
			float sample = random.Next1();
			// sample *= sample;
			// sample = sample <= 0.01f ? 1f : 0f;
			decimal value = (decimal)sample;

			++count;

			decimal oldMean = mean;
			mean += (value - oldMean) / count;
			squared += (value - oldMean) * (value - mean);

			variance = squared / Math.Max(count - 1, 1);
			noise = variance / (decimal)Math.Sqrt(count); // 1 / (count ^ 3) ^ 0.5
		}
		while (count < 16 || noise > Threshold);

		DebugHelper.Log(count, mean, variance, noise);
	}

	static void TestJitter()
	{
		DebugHelper.Log(new Float4(7f, -0f, -2f, 3f).XYZ);

		// var method = typeof(AxisAlignedBoundingBox4).GetMethod(nameof(AxisAlignedBoundingBox4.Intersect));
		// DebugHelper.Log(method.ToAsm());

		var property = typeof(AxisAlignedBoundingBox4).GetProperty(nameof(AxisAlignedBoundingBox4.Encapsulated));
		DebugHelper.Log(property!.GetMethod.ToAsm());

		// var method = typeof(MathF).GetMethod(nameof(Round));
		// DebugHelper.Log(method.ToAsm());
	}

	static unsafe void TestUnmanaged()
	{
		CreateTrash();

		byte[] array = new byte[1024];

		nint truth0;
		nint truth1;

		nint ref0;
		nint ref1;

		ref Reference allocated = ref GC.AllocateArray<Reference>(1, true)[0];

		ref Reference r = ref Unsafe.As<Reference, Reference>(ref allocated);

		r = new Reference(array);

		fixed (byte* ptr = array) truth0 = (nint)ptr;
		fixed (byte* ptr = r.reference) ref0 = (nint)ptr;

		GC.Collect();

		fixed (byte* ptr = array) truth1 = (nint)ptr;
		fixed (byte* ptr = r.reference) ref1 = (nint)ptr;

		DebugHelper.Log(truth1, truth0, truth1 - truth0);
		DebugHelper.Log(ref1, ref0, ref1 - ref0);

		static void CreateTrash()
		{
			byte[] trash = null;

			for (int i = 0; i < 1024; i++) trash = new byte[1024];

			GC.KeepAlive(trash);
		}
	}

	readonly struct Reference
	{
		public Reference(byte[] reference) => this.reference = reference;

		public readonly byte[] reference;
	}
}

// public static class Program
// {
// 	public static void Main()
// 	{
// 		// Builds a context that has all possible accelerators.
// 		using Context context = Context.CreateDefault();
//
// 		// Builds a context that only has CPU accelerators.
// 		//using Context context = Context.Create(builder => builder.CPU());
//
// 		// Builds a context that only has Cuda accelerators.
// 		//using Context context = Context.Create(builder => builder.Cuda());
//
// 		// Builds a context that only has OpenCL accelerators.
// 		//using Context context = Context.Create(builder => builder.OpenCL());
//
// 		// Builds a context with only OpenCL and Cuda acclerators.
// 		//using Context context = Context.Create(builder =>
// 		//{
// 		//    builder
// 		//        .OpenCL()
// 		//        .Cuda();
// 		//});
//
// 		// Prints all accelerators.
// 		foreach (Device d in context)
// 		{
// 			using Accelerator accelerator = d.CreateAccelerator(context);
// 			Console.WriteLine(accelerator);
// 			Console.WriteLine(GetInfoString(accelerator));
// 		}
//
// 		// Prints all CPU accelerators.
// 		foreach (CPUDevice d in context.GetCPUDevices())
// 		{
// 			using CPUAccelerator accelerator = (CPUAccelerator)d.CreateAccelerator(context);
// 			Console.WriteLine(accelerator);
// 			Console.WriteLine(GetInfoString(accelerator));
// 		}
//
// 		// Prints all Cuda accelerators.
// 		foreach (Device d in context.GetCudaDevices())
// 		{
// 			using Accelerator accelerator = d.CreateAccelerator(context);
// 			Console.WriteLine(accelerator);
// 			Console.WriteLine(GetInfoString(accelerator));
// 		}
//
// 		// Prints all OpenCL accelerators.
// 		foreach (Device d in context.GetCLDevices())
// 		{
// 			using Accelerator accelerator = d.CreateAccelerator(context);
// 			Console.WriteLine(accelerator);
// 			Console.WriteLine(GetInfoString(accelerator));
// 		}
// 	}
//
// 	private static string GetInfoString(Accelerator a)
// 	{
// 		StringWriter infoString = new StringWriter();
// 		a.PrintInformation(infoString);
// 		return infoString.ToString();
// 	}
// }