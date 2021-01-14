// |         Method |      Mean |     Error |    StdDev |
// |--------------- |----------:|----------:|----------:|
// |     DivideBase | 1.7661 ns | 0.0132 ns | 0.0124 ns |
// | SeeSharpDivide | 0.0314 ns | 0.0058 ns | 0.0051 ns |
// |  DividePointer | 0.7884 ns | 0.0069 ns | 0.0065 ns |
// |     DivideLoad | 0.5036 ns | 0.0099 ns | 0.0093 ns |
// | DividePointerX | 0.6094 ns | 0.0110 ns | 0.0103 ns |
// |    DivideLoadX | 0.5702 ns | 0.0086 ns | 0.0076 ns |
// |  DivideWrapper | 0.5723 ns | 0.0117 ns | 0.0110 ns |

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CodeHelpers.Diagnostics;

namespace IntrinsicsSIMD
{
	public class Program
	{
		static void Main()
		{
			BenchmarkRunner.Run<Program>();
		}

		const float x = 5.64513f;
		const float y = 0.31234f;
		const float z = 7.29368f;
		const float w = 6.86414f;

		const float a = 12345.64513f;
		const float b = 34560.31234f;
		const float c = 25427.29368f;
		const float d = 62345.86414f;

		static readonly Float4 float0 = new(x, y, z, w);
		static readonly Float4 float1 = new(a, b, c, d);

		static readonly Vector4 vector0 = new(x, y, z, w);
		static readonly Vector4 vector1 = new(a, b, c, d);

		[Benchmark]
		public Vector4 SeeSharpDivide() => vector0 / vector1;

		[Benchmark]
		public Vector4 SeeSharpDivideBase() => SeeSharpDivideBase(vector0, vector1);

		// [Benchmark]
		// public Float4 DivideBase() => DivideBase(float0, float1);
		//
		// [Benchmark]
		// public Float4 DividePointer() => DividePointer(float0, float1);
		//
		// [Benchmark]
		// public Float4 DivideLoad() => DivideLoad(float0, float1);
		//
		// [Benchmark]
		// public Float4 DividePointerX() => DividePointerX(float0, float1);

		[Benchmark]
		public Float4 DivideLoadX() => DivideLoadX(float0, float1);

		[Benchmark]
		public Float4 DivideFuse() => DivideFuse(float0, float1);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector4 SeeSharpDivideBase(Vector4 first, Vector4 second) => first / second;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Float4 DivideBase(in Float4 first, in Float4 second)
		{
			return new(first.x / second.x, first.y / second.y, first.z / second.z, first.w / second.w);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe Float4 DividePointer(Float4 first, Float4 second)
		{
			Vector128<float> firstVector = *(Vector128<float>*)&first;
			Vector128<float> secondVector = *(Vector128<float>*)&second;

			Vector128<float> result = Sse.Divide(firstVector, secondVector);
			return *(Float4*)&result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe Float4 DivideLoad(Float4 first, Float4 second)
		{
			Vector128<float> firstVector = Sse.LoadVector128((float*)&first);
			Vector128<float> secondVector = Sse.LoadVector128((float*)&second);

			Vector128<float> result = Sse.Divide(firstVector, secondVector);
			return *(Float4*)&result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe Float4 DividePointerX(Float4 first, Float4 second)
		{
			Vector128<float> firstVector = *(Vector128<float>*)&first.x;
			Vector128<float> secondVector = *(Vector128<float>*)&second.x;

			Vector128<float> result = Sse.Divide(firstVector, secondVector);
			return *(Float4*)&result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe Float4 DivideLoadX(Float4 first, Float4 second)
		{
			Vector128<float> firstVector = Sse.LoadVector128(&first.x);
			Vector128<float> secondVector = Sse.LoadVector128(&second.x);

			Vector128<float> result = Sse.Divide(firstVector, secondVector);
			return *(Float4*)&result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe Float4 DivideWrapper(Float4 first, Float4 second)
		{
			Vector4 firstVector = *(Vector4*)&first;
			Vector4 secondVector = *(Vector4*)&second;

			Vector4 result = firstVector / secondVector;
			return *(Float4*)&result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe Float4 DivideFuse(Float4 first, Float4 second)
		{
			Vector128<float> result = Sse.Divide(first.vector, second.vector);
			return *(Float4*)&result;
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = 16)]
	public readonly struct Float4
	{
		public Float4(float x, float y, float z, float w)
		{
			vector = default;

			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}

		public Float4(Vector128<float> vector)
		{
			x = default;
			y = default;
			z = default;
			w = default;

			this.vector = vector;
		}

		[FieldOffset(0)] public readonly float x;
		[FieldOffset(4)] public readonly float y;
		[FieldOffset(8)] public readonly float z;
		[FieldOffset(12)] public readonly float w;

		[FieldOffset(0)] internal readonly Vector128<float> vector;
	}
}