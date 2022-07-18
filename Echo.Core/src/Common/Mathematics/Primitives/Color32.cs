using System;
using System.Runtime.InteropServices;
using Echo.Core.Common.Packed;

namespace Echo.Core.Common.Mathematics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Color32 : IEquatable<Color32>
{
	public Color32(byte r, byte g, byte b, byte a = byte.MaxValue)
	{
		this.r = r;
		this.g = g;
		this.b = b;
		this.a = a;
	}

	public Color32(float r, float g, float b, float a = 1f) : this(ToInteger(r), ToInteger(g), ToInteger(b), ToInteger(a)) { }

	public readonly byte r;
	public readonly byte g;
	public readonly byte b;
	public readonly byte a;

	public float RFloat => ToDecimal(r);
	public float GFloat => ToDecimal(g);
	public float BFloat => ToDecimal(b);
	public float AFloat => ToDecimal(a);

	public byte this[int index]
	{
		get
		{
			unsafe
			{
				if ((uint)index > 3) throw new ArgumentOutOfRangeException(nameof(index));
				fixed (Color32* pointer = &this) return ((byte*)pointer)[index];
			}
		}
	}

	public static readonly Color32 black = new(0, 0, 0);
	public static readonly Color32 white = new(byte.MaxValue, byte.MaxValue, byte.MaxValue);

	static float ToDecimal(byte value) => (float)value / byte.MaxValue;
	static byte ToInteger(float value) => (byte)(value * byte.MaxValue).Round().Clamp(0, byte.MaxValue);

	public static explicit operator Color32(Float3 value) => new(value.X, value.Y, value.Z);
	public static explicit operator Color32(Int3 value) => new(value.X, value.Y, value.Z);

	public static explicit operator Float3(Color32 value) => new(value.RFloat, value.GFloat, value.BFloat);
	public static explicit operator Int3(Color32 value) => new(value.r, value.g, value.b);

	public static explicit operator Color32(Float4 value) => new(value.X, value.Y, value.Z, value.W);

	public static explicit operator Float4(Color32 value) => new(value.RFloat, value.GFloat, value.BFloat, value.AFloat);
	public static explicit operator Color64(Color32 value) => new(value.RFloat, value.GFloat, value.BFloat, value.AFloat);

	public static bool operator ==(Color32 first, Color32 second) => first.Equals(second);
	public static bool operator !=(Color32 first, Color32 second) => !first.Equals(second);

	public bool Equals(Color32 other) => r == other.r && g == other.g && b == other.b && a == other.a;
	public override bool Equals(object obj) => obj is Color32 other && Equals(other);

	public override int GetHashCode() => (r << 24) | (g << 16) | (b << 8) | a;
	public override string ToString() => $"{nameof(r)}: {r}, {nameof(g)}: {g}, {nameof(b)}: {b}, {nameof(a)}: {a}";
}