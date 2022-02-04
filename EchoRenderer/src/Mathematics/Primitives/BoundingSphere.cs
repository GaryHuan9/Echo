using System.Runtime.InteropServices;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Mathematics.Primitives;

[StructLayout(LayoutKind.Explicit, Size = 16)]
public readonly struct BoundingSphere
{
	public BoundingSphere(in Float3 center, in float radius)
	{
		this.center = center;
		this.radius = radius;
	}

	[FieldOffset(0)]  public readonly Float3 center;
	[FieldOffset(12)] public readonly float  radius;
}