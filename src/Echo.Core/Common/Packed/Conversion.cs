using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Echo.Core.Common.Packed;

partial struct Float2
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Int2(Float2 value) => new((int)value.X, (int)value.Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Int3(Float2 value) => (Int3)(Int2)value;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Int4(Float2 value) => (Int4)(Int2)value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Float2(float value) => new(value, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Float3(Float2 value) => new(value.X, value.Y, 0f);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Float4(Float2 value) => new(value.X, value.Y, 0f, 0f);
}

partial struct Float3
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Int2(Float3 value) => (Int2)(Int3)value;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Int3(Float3 value) => new((int)value.X, (int)value.Y, (int)value.Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Int4(Float3 value) => (Int4)(Int3)value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Float2(Float3 value) => Unsafe.As<Float3, Float2>(ref Unsafe.AsRef(value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Float3(float value) => new(value, value, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Float4(Float3 value) => new(value.X, value.Y, value.Z, 0f);
}

partial struct Float4
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Int2(Float4 value) => (Int2)(Int4)value;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Int3(Float4 value) => (Int3)(Int4)value;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Int4(Float4 value) => new((int)value.X, (int)value.Y, (int)value.Z, (int)value.W);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Float2(Float4 value) => Unsafe.As<Float4, Float2>(ref Unsafe.AsRef(value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Float3(Float4 value) => Unsafe.As<Float4, Float3>(ref Unsafe.AsRef(value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Float4(float value) => new(Vector128.Create(value));
}

partial struct Int2
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Int2(int value) => new(value, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Int3(Int2 value) => new(value.X, value.Y, 0);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Int4(Int2 value) => new(value.X, value.Y, 0, 0);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator Float2(Int2 value) => new(value.X, value.Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Float3(Int2 value) => (Float3)(Float2)value;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Float4(Int2 value) => (Float4)(Float2)value;
}

partial struct Int3
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Int2(Int3 value) => Unsafe.As<Int3, Int2>(ref Unsafe.AsRef(value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Int3(int value) => new(value, value, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Int4(Int3 value) => new(value.X, value.Y, value.Z, 0);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Float2(Int3 value) => (Float2)(Float3)value;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator Float3(Int3 value) => new(value.X, value.Y, value.Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Float4(Int3 value) => (Float4)(Float3)value;
}

partial struct Int4
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Int2(Int4 value) => Unsafe.As<Int4, Int2>(ref Unsafe.AsRef(value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Int3(Int4 value) => Unsafe.As<Int4, Int3>(ref Unsafe.AsRef(value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Int4(int value) => new(value, value, value, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Float2(Int4 value) => (Float2)(Float4)value;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static explicit operator Float3(Int4 value) => (Float3)(Float4)value;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator Float4(Int4 value) => new(value.X, value.Y, value.Z, value.W);
}