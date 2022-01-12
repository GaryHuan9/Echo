﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using static EchoRenderer.Common.Utilities;

namespace EchoRenderer.Mathematics.Primitives
{
	[StructLayout(LayoutKind.Explicit, Size = 40)]
	public readonly struct Ray
	{
		/// <summary>
		/// Constructs a ray.
		/// </summary>
		/// <param name="origin">The origin of the ray</param>
		/// <param name="direction">The direction of the ray. NOTE: it should be normalized.</param>
		public Ray(Float3 origin, Float3 direction)
		{
			Assert.AreEqual(direction.SquaredMagnitude, 1f);

			Unsafe.SkipInit(out originVector);
			Unsafe.SkipInit(out directionVector);
			Unsafe.SkipInit(out inverseDirection);

			this.origin = origin;
			this.direction = direction;

			//Because _mm_rcp_ps is only an approximation, we cannot use it here
			inverseDirectionVector = Sse.Divide(Vector128.Create(1f), directionVector);
		}

		[FieldOffset(0)] public readonly Float3 origin;
		[FieldOffset(12)] public readonly Float3 direction;
		[FieldOffset(24)] public readonly Float3 inverseDirection;

		//NOTE: these fields have overlapping memory offsets to reduce footprint. Pay extra attention when assigning them.
		[FieldOffset(0)] public readonly Vector128<float> originVector;
		[FieldOffset(12)] public readonly Vector128<float> directionVector;
		[FieldOffset(24)] public readonly Vector128<float> inverseDirectionVector;

		/// <summary>
		/// Returns the point this <see cref="Ray"/> points at <paramref name="distance"/>.
		/// </summary>
		public unsafe Float3 GetPoint(float distance)
		{
			Vector128<float> length = Vector128.Create(distance);
			Vector128<float> result = Fused(directionVector, length, originVector);

			return *(Float3*)&result;
		}

		public override string ToString() => $"{nameof(origin)}: {origin}, {nameof(direction)}: {direction}";
	}
}