using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Mathematics.Intersections
{
	[StructLayout(LayoutKind.Explicit, Size = 48)]
	public readonly struct Ray
	{
		/// <summary>
		/// Constructs a ray.
		/// </summary>
		/// <param name="origin">The origin of the ray</param>
		/// <param name="direction">The direction of the ray. NOTE: it should be normalized.</param>
		public Ray(Float3 origin, Float3 direction)
		{
			Debug.Assert(Scalars.AlmostEquals(direction.SquaredMagnitude, 1f));

			Unsafe.SkipInit(out originVector);
			Unsafe.SkipInit(out directionVector);
			Unsafe.SkipInit(out inverseDirection);

			this.origin = origin;
			this.direction = direction;

			Vector128<float> reciprocalVector = Sse.Divide(oneVector, directionVector); //Because _mm_rcp_ps is only an approximation, we cannot use it here
			inverseDirectionVector = Sse.Min(maxValueVector, Sse.Max(minValueVector, reciprocalVector));

			Vector128<float> negated = Sse.Subtract(Vector128<float>.Zero, inverseDirectionVector);
			absolutedInverseDirectionVector = Sse.Max(negated, inverseDirectionVector);
		}

		[FieldOffset(0)] public readonly Float3 origin;
		[FieldOffset(12)] public readonly Float3 direction;
		[FieldOffset(24)] public readonly Float3 inverseDirection;

		//NOTE: these fields have overlapping memory offsets to reduce footprint. Pay extra attention when assigning them.
		[FieldOffset(0)] public readonly Vector128<float> originVector;
		[FieldOffset(12)] public readonly Vector128<float> directionVector;
		[FieldOffset(24)] public readonly Vector128<float> inverseDirectionVector;
		[FieldOffset(36)] public readonly Vector128<float> absolutedInverseDirectionVector;

		static readonly Vector128<float> minValueVector = Vector128.Create(float.MinValue);
		static readonly Vector128<float> maxValueVector = Vector128.Create(float.MaxValue);
		static readonly Vector128<float> oneVector = Vector128.Create(1f);

		public unsafe Float3 GetPoint(float distance)
		{
			if (Avx.IsSupported)
			{
				Vector128<float> length = Sse.Multiply(directionVector, Avx.BroadcastScalarToVector128(&distance));
				Vector128<float> result = Sse.Add(originVector, length);

				return *(Float3*)&result;
			}

			return origin + direction * distance;
		}

		public override string ToString() => $"{nameof(origin)}: {origin}, {nameof(direction)}: {direction}";
	}
}