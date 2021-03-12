using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;
using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
using ForceRenderer.Objects.SceneObjects;

namespace IntrinsicsSIMD
{
	public class BenchmarkAABB
	{
		public BenchmarkAABB()
		{
			triangle = new PressedTriangle(new Float3(1f, 1f, 0f), new Float3(-1f, -1f, -1f), new Float3(0f, 1f, 1f), 0);

			aabb = new AxisAlignedBoundingBox(triangle.AABB);
			rays = new Ray[65536];

			Random random = new Random(42);

			for (int i = 0; i < rays.Length; i++)
			{
				Float3 point0 = GetPoint();
				Float3 point1 = GetPoint();

				rays[i] = new Ray(point0, (point1 - point0).Normalized);
			}

			Float3 GetPoint()
			{
				Float3 scale = new Float3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
				return aabb.center + (scale * 2f - Float3.one) * aabb.extend * 2f;
			}
		}

		readonly Ray[] rays;

		readonly PressedTriangle triangle;
		readonly AxisAlignedBoundingBox aabb;

		//	     IntersectOld | 1.214 ms per 65536 = 20.8 ns
		//	    IntersectSIMD | 1.133 ms per 65536 = 19.2 ns
		//	IntersectPureSIMD | 0.739 ms per 65536 = 11.3 ns
		//	IntersectTriangle | 1.724 ms per 65536 = 26.3 ns

		[Benchmark]
		public float IntersectOld()
		{
			float result = 0f;

			for (int i = 0; i < rays.Length; i++) result = aabb.IntersectOld(rays[i]);

			return result;
		}

		[Benchmark]
		public float IntersectSIMD()
		{
			float result = 0f;

			for (int i = 0; i < rays.Length; i++) result = aabb.IntersectSIMD(rays[i]);

			return result;
		}

		[Benchmark]
		public float IntersectPureSIMD()
		{
			float result = 0f;

			for (int i = 0; i < rays.Length; i++) result = aabb.IntersectPureSIMD(rays[i]);

			return result;
		}

		[Benchmark]
		public float IntersectTriangle()
		{
			float result = 0f;
			Float2 uv = default;

			for (int i = 0; i < rays.Length; i++) result = triangle.GetIntersection(rays[i], out uv);

			return result + uv.x;
		}

		[StructLayout(LayoutKind.Explicit, Size = 28)]
		public readonly struct AxisAlignedBoundingBox
		{
			public AxisAlignedBoundingBox(Float3 center, Float3 extend)
			{
				centerVector = default;
				extendVector = default;

				this.center = center;
				this.extend = extend;
			}

			public AxisAlignedBoundingBox(ForceRenderer.Mathematics.AxisAlignedBoundingBox aabb) : this(aabb.center, aabb.extend) { }

			[FieldOffset(0)] public readonly Float3 center;  //The exact center of the box
			[FieldOffset(12)] public readonly Float3 extend; //Half the size of the box

			[FieldOffset(0)] readonly Vector128<float> centerVector;
			[FieldOffset(12)] readonly Vector128<float> extendVector;

			public Float3 Max => center + extend;
			public Float3 Min => center - extend;

			public float Area => (extend.x * extend.y + extend.x * extend.z + extend.y * extend.z) * 8f;

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

			public unsafe float IntersectPureSIMD(in Ray ray)
			{
				Vector128<float> n = Sse.Multiply(ray.inverseDirectionVector, Sse.Subtract(centerVector, ray.originVector));
				Vector128<float> k = Sse.Multiply(ray.absolutedInverseDirectionVector, extendVector);

				Vector128<float> min = Sse.Add(n, k);
				Vector128<float> max = Sse.Subtract(n, k);

				//Permute vector for min max, ignores last component
				Vector128<float> minPermute = Avx.Permute(min, 0b0100_1010);
				Vector128<float> maxPermute = Avx.Permute(max, 0b0100_1010);

				min = Sse.Min(min, minPermute);
				max = Sse.Max(max, maxPermute);

				//Second permute for min max
				minPermute = Avx.Permute(min, 0b1011_0001);
				maxPermute = Avx.Permute(max, 0b1011_0001);

				min = Sse.Min(min, minPermute);
				max = Sse.Max(max, maxPermute);

				//Extract result
				float far = *(float*)&min;
				float near = *(float*)&max;

				return near > far || far < 0f ? float.PositiveInfinity : near;
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
}