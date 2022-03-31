using System.Runtime.Intrinsics;
using CodeHelpers.Packed;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;

namespace EchoRenderer.Core.Texturing;

/// <summary>
/// A custom linear transform that can be applied to a color.
/// </summary>
public readonly struct Tint
{
	Tint(in Float4 scale, in Float4 offset)
	{
		this.scale = scale;
		this.offset = offset;
	}

	readonly Float4 scale;
	readonly Float4 offset;

	public static readonly Tint identity = new(RGBA32.White, RGBA32.Zero);

	public RGBA32 Apply(in RGBA32 color)
	{
		//TODO: use FMA
		return (RGBA32)(color * scale + offset);
		// return PackedMath.FMA(color, scale, offset);
	}

	public static Tint Scale(in RGBA32 value) => new(value, RGBA32.Zero);
	public static Tint Offset(in RGBA32 value) => new(RGBA32.White, value);

	public static Tint Inverse(in RGBA32 value) => new(-(Float4)value, value);
	public static Tint Inverse() => Inverse(RGBA32.White);
}