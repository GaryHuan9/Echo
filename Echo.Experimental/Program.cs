using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Running;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.Common.Mathematics;
using Echo.Core.Aggregation.Acceleration;
using Echo.Experimental.Benchmarks;
using JitBuddy;

namespace Echo.Experimental;

public class Program
{
	static void Main()
	{
		TestJitter();
		// TestUnmanaged();

		// var test = new AabbSimd();
		//
		// test.Quad();
		// test.Quad2();

		// BenchmarkRunner.Run<PackedFloats>();
		// BenchmarkRunner.Run<Aggregators>();
		// BenchmarkRunner.Run<RadixSort>();
		// BenchmarkRunner.Run<Loops>();
		// BenchmarkRunner.Run<AabbSimd>();
		// BenchmarkRunner.Run<MathFunctions>();
	}

	public static void SinCos(float radians, out float sin, out float cos) => (sin, cos) = MathF.SinCos(radians);

	// 00007FFA114A83D0 vzeroupper
	// 00007FFA114A83D3 vucomiss  xmm0,xmm1
	// 00007FFA114A83D7 seta      al
	// 00007FFA114A83DA movzx     eax,al
	// 00007FFA114A83DD ret

	static void TestJitter()
	{
		// var method = typeof(AxisAlignedBoundingBox4V2).GetMethod(nameof(AxisAlignedBoundingBox4V2.Intersect));
		// DebugHelper.Log(method.ToAsm());

		// var property = typeof(Float4).GetProperty(nameof(Float4.X));
		// DebugHelper.Log(property!.GetMethod.ToAsm());

		var method = typeof(MathF).GetMethod(nameof(SinCos), BindingFlags.NonPublic | BindingFlags.Static);
		DebugHelper.Log(method.ToMethodSignature());
		DebugHelper.Log(method.ToAsm());
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