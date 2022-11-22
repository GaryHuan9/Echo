using System;
using System.Runtime.CompilerServices;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Scenic.Geometries;
using Echo.Core.Scenic.Hierarchies;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Experimental;

public class Program
{
	static void Main()
	{
		var material0 = new Conductor { Roughness = new Pure(new RGBA128(0.0f)) };
		var material1 = new Conductor { Roughness = new Pure(new RGBA128(1.0f)) };

		var scene = new ScenePreparer(new Scene { new SphereEntity() }).Prepare();

		Float3 origin = new Float3(0f, 0f, -4.5f);

		Float4 last = Float4.Zero;

		for (int i = -110; i <= 0; i++)
		{
			Float3 target = new Float3(i / 100f, 0f, 0f);
			var query = new TraceQuery(new Ray(origin, (target - origin).Normalized));

			if (!scene.Trace(ref query)) continue;

			Contact contact = scene.Interact(query);
			Allocator allocator = new Allocator();

			allocator.Begin();

			BSDF bsdf = material1.Scatter(contact, allocator, RGB128.White);

			var distribution = new StratifiedDistribution
			{
				Extend = 1,
				Prng = new SystemPrng(1)
			};

			distribution.BeginSeries(Int2.Zero);
			distribution.BeginSession();

			Summation total = Summation.Zero;
			int count = 0;

			for (int j = 0; j < distribution.Extend; j++)
			{
				var sample = distribution.Next2D();
				// var sample = new Sample2D((Sample1D)0.1234f, (Sample1D)0.562345f);

				var sampled = bsdf.Sample(Float3.Backward, sample, out Float3 incident, out _);

				if (!sampled.NotPossible)
				{
					total += sampled.content / sampled.pdf * incident.Dot(contact.point.normal);
					count++;
				}
			}

			Float4 current = total.Result / count;
			if (count > 0) DebugHelper.Log(i, current - last);
			last = current;
		}

		// Float3 target0 = new Float3(-1f, 0f, 0f);
		// Float3 target1 = new Float3(0f, 0f, 0f);
		//
		// var query0 = new TraceQuery(new Ray(origin, (target0 - origin).Normalized));
		// var query1 = new TraceQuery(new Ray(origin, (target1 - origin).Normalized));
		//
		// DebugHelper.Log(scene.Trace(ref query0), query0.distance);
		// DebugHelper.Log(scene.Trace(ref query1), query1.distance);
		//
		// Contact contact0 = scene.Interact(query0);
		// Contact contact1 = scene.Interact(query1);
		// Allocator allocator = new Allocator();
		//
		// DebugHelper.Log(contact0.point.normal, contact1.point.normal);
		//
		// allocator.Begin();
		//
		// BSDF bsdf0 = conductor1.Scatter(contact0, allocator, RGB128.White);
		// BSDF bsdf1 = conductor1.Scatter(contact1, allocator, RGB128.White);
		//
		// // DebugHelper.Log(bsdf0.Evaluate(Float3.Backward, contact0.point.normal));
		// // DebugHelper.Log(bsdf1.Evaluate(Float3.Backward, contact1.point.normal));
		//
		// var distribution = new StratifiedDistribution
		// {
		// 	Extend = 1024,
		// 	Prng = new SystemPrng(1)
		// };
		//
		// distribution.BeginSeries(Int2.Zero);
		// distribution.BeginSession();
		//
		// int count0 = 0;
		// int count1 = 0;
		//
		// for (int i = 0; i < distribution.Extend; i++)
		// {
		// 	var sample = distribution.Next2D();
		//
		// 	if (bsdf0.Sample(Float3.Backward, sample, out Float3 incident0, out _).NotPossible) count0++;
		// 	if (bsdf1.Sample(Float3.Backward, sample, out Float3 incident1, out _).NotPossible) count1++;
		// }
		//
		// DebugHelper.Log(count0, count1);

		// TestMonteCarlo();
		// TestJitter();
		// TestUnmanaged();

		// BenchmarkRunner.Run<PackedFloats>();
		// BenchmarkRunner.Run<Aggregators>();
		// BenchmarkRunner.Run<Loops>();
		// BenchmarkRunner.Run<BoxBounds>();
		// BenchmarkRunner.Run<MathFunctions>();
		// BenchmarkRunner.Run<BufferCopy>();
		// BenchmarkRunner.Run<Timing>();
	}

	static void TestMonteCarlo()
	{
		Prng random = new SquirrelPrng();

		const double Threshold = 0.001d;

		int count = 0;

		decimal mean = 0m;
		decimal squared = 0m;

		decimal variance;
		double noise;

		do
		{
			float sample = random.Next1() * 100f;
			// sample *= sample;
			decimal value = (decimal)sample;

			++count;

			decimal oldMean = mean;
			mean += (value - oldMean) / count;
			squared += (value - oldMean) * (value - mean);

			variance = squared / Math.Max(count - 1, 1);
			double deviation = Math.Sqrt((double)variance);
			noise = deviation / Math.Sqrt(count) / (double)mean;
		}
		while (count < 16 || noise > Threshold);

		DebugHelper.Log(count, mean, variance, noise);
	}

	static void TestJitter()
	{
		DebugHelper.Log(new Float4(7f, -0f, -2f, 3f).XYZ);

		// var method = typeof(AxisAlignedBoundingBox4).GetMethod(nameof(AxisAlignedBoundingBox4.Intersect));
		// DebugHelper.Log(method.ToAsm());

		// var property = typeof(AxisAlignedBoundingBox4).GetProperty(nameof(AxisAlignedBoundingBox4.Encapsulated));
		// DebugHelper.Log(property!.GetMethod.ToAsm());

		// var method = typeof(Float4x4).GetMethod(nameof(Float4x4.MultiplyPoint));
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