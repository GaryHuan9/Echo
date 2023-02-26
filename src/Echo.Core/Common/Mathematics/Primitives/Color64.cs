using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Echo.Core.Common.Packed;

namespace Echo.Core.Common.Mathematics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Color64 : IEquatable<Color64>
{
	public Color64(ushort r, ushort g, ushort b, ushort a = ushort.MaxValue)
	{
		this.r = r;
		this.g = g;
		this.b = b;
		this.a = a;
	}

	public Color64(float r, float g, float b, float a = 1f) : this(ToInteger(r), ToInteger(g), ToInteger(b), ToInteger(a)) { }

	public readonly ushort r;
	public readonly ushort g;
	public readonly ushort b;
	public readonly ushort a;

	public float RFloat => ToDecimal(r);
	public float GFloat => ToDecimal(g);
	public float BFloat => ToDecimal(b);
	public float AFloat => ToDecimal(a);

	public ushort this[int index]
	{
		get
		{
			unsafe
			{
				if ((uint)index > 3) throw new ArgumentOutOfRangeException(nameof(index));
				fixed (Color64* pointer = &this) return ((ushort*)pointer)[index];
			}
		}
	}

	public static readonly Color64 black = new(0, 0, 0);
	public static readonly Color64 white = new(ushort.MaxValue, ushort.MaxValue, ushort.MaxValue);

	static float ToDecimal(ushort value) => (float)value / ushort.MaxValue;
	static ushort ToInteger(float value) => (ushort)(value.Clamp() * ushort.MaxValue);

	public static explicit operator Color64(Float3 value) => new(value.X, value.Y, value.Z);
	public static explicit operator Color64(Int3 value) => new(value.X, value.Y, value.Z);

	public static explicit operator Float3(Color64 value) => new(value.RFloat, value.GFloat, value.BFloat);
	public static explicit operator Int3(Color64 value) => new(value.r, value.g, value.b);

	public static explicit operator Float4(Color64 value) => new(value.RFloat, value.GFloat, value.BFloat, value.AFloat);
	public static explicit operator Color32(Color64 value) => new(value.RFloat, value.GFloat, value.BFloat, value.AFloat);

	public static bool operator ==(Color64 first, Color64 second) => first.Equals(second);
	public static bool operator !=(Color64 first, Color64 second) => !first.Equals(second);

	public bool Equals(Color64 other) => r == other.r && g == other.g && b == other.b && a == other.a;
	public override bool Equals(object obj) => obj is Color64 other && Equals(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode()
	{
		unchecked
		{
			int hashCode = r.GetHashCode();
			hashCode = (hashCode * 397) ^ g.GetHashCode();
			hashCode = (hashCode * 397) ^ b.GetHashCode();
			hashCode = (hashCode * 397) ^ a.GetHashCode();
			return hashCode;
		}
	}

	public override string ToString() => $"{nameof(r)}: {r}, {nameof(g)}: {g}, {nameof(b)}: {b}, {nameof(a)}: {a}";
}