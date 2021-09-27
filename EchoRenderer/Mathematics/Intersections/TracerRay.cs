using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using static EchoRenderer.Mathematics.Utilities;

namespace EchoRenderer.Mathematics.Intersections
{
	[StructLayout(LayoutKind.Explicit, Size = 32)]
	public readonly struct TracerRay
	{
		public TracerRay(in Float3 origin, in Float3 direction)
		{
			inverseDirectionVector = Sse.Divide(vector1, Vector128.Create(direction.x, direction.y, direction.z, 1f));

			this.origin = origin;

			Unsafe.SkipInit(out inverseDirection);
			Unsafe.SkipInit(out padding0);
			Unsafe.SkipInit(out padding1);
		}

		[FieldOffset(00)] public readonly Float3 origin;
		[FieldOffset(16)] public readonly Float3 inverseDirection;

		[FieldOffset(12)] readonly float padding0;
		[FieldOffset(28)] readonly float padding1;

		[FieldOffset(16)] readonly Vector128<float> inverseDirectionVector;
	}
}