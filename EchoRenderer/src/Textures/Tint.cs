using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;

namespace EchoRenderer.Textures;

/// <summary>
/// A custom linear transform that can be applied to a color.
/// </summary>
public readonly struct Tint
{
	Tint(in Float4 scale, in Float4 offset)
	{
		this.scale = Utilities.ToVector(scale);
		this.offset = Utilities.ToVector(offset);
	}

	readonly Vector128<float> scale;
	readonly Vector128<float> offset;

	public static readonly Tint identity = new(Float4.one, Float4.zero);

	public Vector128<float> Apply(in Vector128<float> color) => PackedMath.FMA(color, scale, offset);

	public static Tint Scale(in Float4 value) => new(value, Float4.zero);
	public static Tint Scale(in Float3 value) => Scale(Utilities.ToColor(value));

	public static Tint Offset(in Float4 value) => new(Float4.one, value);
	public static Tint Offset(in Float3 value) => Offset((Float4)value);

	public static Tint Inverse(in Float4 value) => new(-value, value);
	public static Tint Inverse(in Float3 value) => Inverse(Utilities.ToColor(value));
	public static Tint Inverse() => Inverse(Float4.one);
}