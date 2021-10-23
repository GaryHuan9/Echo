using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;

// |             Method |      Mean |     Error |    StdDev |
// |------------------- |----------:|----------:|----------:|
// |     SeeSharpDivide | 0.0816 ns | 0.0031 ns | 0.0025 ns |
// | SeeSharpDivideBase | 0.3092 ns | 0.0089 ns | 0.0083 ns |
// |         DivideBase | 7.1403 ns | 0.0455 ns | 0.0426 ns |
// |      DividePointer | 0.6697 ns | 0.0122 ns | 0.0114 ns |
// |         DivideLoad | 0.9341 ns | 0.0185 ns | 0.0173 ns |
// |     DividePointerX | 0.7699 ns | 0.0159 ns | 0.0148 ns |
// |        DivideLoadX | 0.6562 ns | 0.0098 ns | 0.0092 ns |
// |         DivideFuse | 0.9338 ns | 0.0120 ns | 0.0112 ns |

namespace EchoRenderer.Experimental
{
	public class TestSIMD
	{
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



		// [Benchmark]
		public Vector4 SeeSharpDivide() => vector0 / vector1;

		// [Benchmark]
		public Vector4 SeeSharpDivideBase() => SeeSharpDivideBase(vector0, vector1);

		[Benchmark]
		public Float4 DivideBase() => DivideBase(float0, float1);

		[Benchmark]
		public Float4 DivideLoadX() => DivideLoadX(float0, float1); //Winner

		[Benchmark]
		public Float4 DivideStoreX() => DivideStoreX(float0, float1); //Winner

		[Benchmark]
		public Float4 DivideFuse() => DivideFuse(float0, float1);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector4 SeeSharpDivideBase(Vector4 first, Vector4 second) => first / second;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Float4 DivideBase(in Float4 first, in Float4 second)
		{
			return new(first.x / second.x, first.y / second.y, first.z / second.z, 0f);
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
		public static unsafe Float4 DivideStoreX(Float4 first, Float4 second)
		{
			Vector128<float> firstVector = Sse.LoadVector128(&first.x);
			Vector128<float> secondVector = Sse.LoadVector128(&second.x);

			Float4 result;
			Sse.Store((float*)&result, Sse.Divide(firstVector, secondVector));
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe Float4 DivideFuse(Float4 first, Float4 second)
		{
			Vector128<float> result = Sse.Divide(first.vector, second.vector);
			return *(Float4*)&result;
		}

		[StructLayout(LayoutKind.Explicit, Size = 12)]
		public readonly struct Float4
		{
			public Float4(float x, float y, float z, float w)
			{
				vector = default;

				this.x = x;
				this.y = y;
				this.z = z;
				// this.w = w;
			}

			public Float4(Vector128<float> vector)
			{
				x = default;
				y = default;
				z = default;
				// w = default;

				this.vector = vector;
			}

			[FieldOffset(0)] public readonly float x;
			[FieldOffset(4)] public readonly float y;
			[FieldOffset(8)] public readonly float z;
			// [FieldOffset(12)] public readonly float w;

			[FieldOffset(0)] internal readonly Vector128<float> vector;

			public override string ToString() => $"{nameof(x)}: {x}, {nameof(y)}: {y}, {nameof(z)}: {z}";
		}
	}
}