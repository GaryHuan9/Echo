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

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CodeHelpers.Mathematics;

namespace IntrinsicsSIMD
{
	public class Program
	{
		static void Main()
		{
			// BenchmarkRunner.Run<Program>();
			BenchmarkRunner.Run<RayBenchmark>();

			// DebugHelper.Log(DivideBase(new Float4(1f, 2f, 3f, 4f), new Float4(5f, 6f, 7f, 8f)));
			// DebugHelper.Log(DivideLoadX(new Float4(1f, 2f, 3f, 4f), new Float4(5f, 6f, 7f, 8f)));

			Console.ReadKey();
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

	public class RayBenchmark
	{
		readonly Ray ray = new(Float3.up, Float3.down);
		readonly AxisAlignedBoundingBox aabb = new(Float3.zero, Float3.half);

		[Benchmark]
		public float IntersectOld() => aabb.IntersectOld(ray);

		[Benchmark]
		public float IntersectSIMD() => aabb.IntersectSIMD(ray);
	}

	[StructLayout(LayoutKind.Explicit, Size = 64)]
	public readonly struct Ray
	{
		/// <summary>
		/// Constructs a ray.
		/// </summary>
		/// <param name="origin">The origin of the ray</param>
		/// <param name="direction">The direction of the ray. NOTE: it should be normalized.</param>
		public Ray(Float3 origin, Float3 direction)
		{
			originVector = default;
			directionVector = default;
			inverseDirection = default;

			this.origin = origin;
			this.direction = direction;

			Vector128<float> reciprocalVector = Sse.Reciprocal(directionVector);
			inverseDirectionVector = Sse.Min(maxValueVector, Sse.Max(minValueVector, reciprocalVector));

			Vector128<float> negated = Sse.Subtract(Vector128<float>.Zero, inverseDirectionVector);
			absolutedInverseDirectionVector = Sse.Max(negated, inverseDirectionVector);
		}

		[FieldOffset(0)] public readonly Float3 origin;
		[FieldOffset(16)] public readonly Float3 direction;
		[FieldOffset(32)] public readonly Float3 inverseDirection;

		[FieldOffset(0)] public readonly Vector128<float> originVector;
		[FieldOffset(16)] public readonly Vector128<float> directionVector;
		[FieldOffset(32)] public readonly Vector128<float> inverseDirectionVector;
		[FieldOffset(48)] public readonly Vector128<float> absolutedInverseDirectionVector;

		static readonly Vector128<float> minValueVector = Vector128.Create(float.MinValue, float.MinValue, float.MinValue, float.MinValue);
		static readonly Vector128<float> maxValueVector = Vector128.Create(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue);

		public Float3 GetPoint(float distance) => origin + direction * distance;

		public override string ToString() => $"{nameof(origin)}: {origin}, {nameof(direction)}: {direction}";
	}

	[StructLayout(LayoutKind.Explicit, Size = 32)]
	public readonly struct AxisAlignedBoundingBox
	{
		public AxisAlignedBoundingBox(Float3 center, Float3 extend)
		{
			centerVector = default;
			extendVector = default;

			this.center = center;
			this.extend = extend;
		}

		[FieldOffset(0)] public readonly Float3 center;  //The exact center of the box
		[FieldOffset(16)] public readonly Float3 extend; //Half the size of the box

		[FieldOffset(0)] readonly Vector128<float> centerVector;
		[FieldOffset(16)] readonly Vector128<float> extendVector;

		public Float3 Max => center + extend;
		public Float3 Min => center - extend;

		public float Area => (extend.x * extend.y + extend.x * extend.z + extend.y * extend.z) * 8f;

		/// <summary>
		/// Tests intersection with bounding box. Returns distance to the nearest intersection point.
		/// NOTE: return can be negative, which means the ray origins inside box.
		/// </summary>
		public float IntersectSIMD(in Ray ray)
		{
			Vector128<float> n = Sse.Multiply(ray.inverseDirectionVector, Sse.Subtract(centerVector, ray.originVector));
			Vector128<float> k = Sse.Multiply(ray.absolutedInverseDirectionVector, extendVector);

			Vector128<float> min = Sse.Add(n, k);
			Vector128<float> max = Sse.Subtract(n, k);

			unsafe
			{
				float far = (*(Float3*)&min).MinComponent;
				float near = (*(Float3*)&max).MaxComponent;

				return near > far || far < 0f ? float.PositiveInfinity : near;
			}
		}

		public float IntersectOld(in Ray ray)
		{
			Float3 n = ray.inverseDirection * (center - ray.origin);
			Float3 k = ray.inverseDirection.Absoluted * extend;

			float near = (n - k).MaxComponent;
			float far = (n + k).MinComponent;

			return near > far || far < 0f ? float.PositiveInfinity : near;
		}
	}
}