using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;
using CodeHelpers.Mathematics;

namespace IntrinsicsSIMD
{
	public class BenchmarkAABB
	{
		readonly Ray ray = new(Float3.up, Float3.down);
		readonly AxisAlignedBoundingBox aabb = new(Float3.zero, Float3.half);

		[Benchmark]
		public float IntersectOld() => aabb.IntersectOld(ray);

		[Benchmark]
		public float IntersectSIMD() => aabb.IntersectSIMD(ray);

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

			[FieldOffset(0)] public readonly Float3 center;  //The exact center of the box
			[FieldOffset(12)] public readonly Float3 extend; //Half the size of the box

			[FieldOffset(0)] readonly Vector128<float> centerVector;
			[FieldOffset(12)] readonly Vector128<float> extendVector;

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
}