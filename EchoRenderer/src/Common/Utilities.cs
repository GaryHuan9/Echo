using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Common
{
	public static class Utilities
	{
		public static Float4 ToFloat4(Vector128<float> pixel) => Unsafe.As<Vector128<float>, Float4>(ref pixel);
		public static Float3 ToFloat3(Vector128<float> pixel) => Unsafe.As<Vector128<float>, Float3>(ref pixel);

		public static ref Float4 ToFloat4(ref Vector128<float> pixel) => ref Unsafe.As<Vector128<float>, Float4>(ref pixel);
		public static ref Float3 ToFloat3(ref Vector128<float> pixel) => ref Unsafe.As<Vector128<float>, Float3>(ref pixel);

		public static Vector128<float> ToVector(Float4 value) => Unsafe.As<Float4, Vector128<float>>(ref value);
		public static Vector128<float> ToVector(Float3 value) => Unsafe.As<Float3, Vector128<float>>(ref value);

		//NOTE: to vector with ref values is unsafe and not provided here since we can access weird memory with it.

		public static Float4 ToColor(float     value) => ToColor((Float4)value);
		public static Float4 ToColor(in Float3 value) => ToColor((Float4)value);
		public static Float4 ToColor(in Float4 value) => value.Replace(3, 1f);

		public static Float4 ToColor(ReadOnlySpan<char> value)
		{
			value = value.Trim();
			if (value[0] == '#') value = value[1..];

			switch (value.Length)
			{
				case 3: return (Float4)new Color32(ParseOne(value[0]), ParseOne(value[1]), ParseOne(value[2]));
				case 4: return (Float4)new Color32(ParseOne(value[0]), ParseOne(value[1]), ParseOne(value[2]), ParseOne(value[3]));

				case 6: return (Float4)new Color32(ParseTwo(value[..2]), ParseTwo(value[2..4]), ParseTwo(value[4..]));
				case 8: return (Float4)new Color32(ParseTwo(value[..2]), ParseTwo(value[2..4]), ParseTwo(value[4..6]), ParseTwo(value[6..]));
			}

			throw ExceptionHelper.Invalid(nameof(value), value.ToString(), "is not valid hex");

			static byte ParseOne(char value)
			{
				int hex = ParseHex(value);
				return (byte)(hex * 16 + hex);
			}

			static byte ParseTwo(ReadOnlySpan<char> value) => (byte)(ParseHex(value[0]) * 16 + ParseHex(value[1]));

			static int ParseHex(char value)
			{
				if ('0' <= value && value <= '9') return value & 0b1111;
				if ('a' <= value && value <= 'f') value -= (char)('a' - 'A');

				if ('A' <= value && value <= 'F') return value - 'A' + 10;
				throw ExceptionHelper.Invalid(nameof(value), value, "is not valid hex");
			}
		}

		/// <summary>
		/// Linearly interpolates between <paramref name="left"/> and <paramref name="right"/> based on <paramref name="time"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector128<float> Lerp(in Vector128<float> left, in Vector128<float> right, in Vector128<float> time)
		{
			var length = Sse.Subtract(right, left);
			return Fused(length, time, left);
		}

		/// <summary>
		/// Numerically clamps <paramref name="value"/> between <paramref name="min"/> and <paramref name="max"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector128<float> Clamp(in Vector128<float> value, in Vector128<float> min, in Vector128<float> max) => Sse.Min(max, Sse.Max(min, value));

		/// <summary>
		/// Numerically clamps <paramref name="value"/> between zero and one.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector128<float> Clamp01(in Vector128<float> value) => Clamp(value, Vector128.Create(0f), Vector128.Create(1f));

		/// <summary>
		/// Fused multiply and add <paramref name="value"/> with <paramref name="multiplier"/> first and then <paramref name="adder"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector128<float> Fused(in Vector128<float> value, in Vector128<float> multiplier, in Vector128<float> adder)
		{
			if (Fma.IsSupported) return Fma.MultiplyAdd(value, multiplier, adder);
			return Sse.Add(Sse.Multiply(value, multiplier), adder);
		}

		/// <summary>
		/// Returns the approximated luminance of <paramref name="color"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float GetLuminance(in Vector128<float> color)
		{
			if (Sse41.IsSupported) return Sse41.DotProduct(color, Constants.LuminanceVector, 0b0111_0001).GetElement(0);

			Vector128<float> result = Sse.Multiply(color, Constants.LuminanceVector);
			return result.GetElement(0) + result.GetElement(1) + result.GetElement(2);
		}

		/// <summary>
		/// Returns a color with minimal individual component sum that has <paramref name="luminance"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector128<float> CreateLuminance(float luminance) => Sse.Multiply(Constants.LuminanceVector, Vector128.Create(luminance));

		/// <summary>
		/// If <paramref name="index"/> is valid for <paramref name="span"/>, returns
		/// the item it points. Otherwise, <paramref name="defaultValue"/> is returned.
		/// </summary>
		public static ref readonly T TryGetValue<T>(this ReadOnlySpan<T> span, int index, in T defaultValue = default)
		{
			if (0 <= index && index < span.Length) return ref span[index];
			return ref defaultValue;
		}

		/// <summary>
		/// Calculates and returns a deterministic hash code for <paramref name="value"/>.
		/// </summary>
		public static unsafe int GetHashCode<T>(Vector128<T> value) where T : struct => GetHashCode(&value);

		/// <inheritdoc cref="GetHashCode(byte*,uint,int)"/>
		public static unsafe int GetHashCode<T>(T* ptr, uint length = 1, int initial = 0) where T : unmanaged => GetHashCode((byte*)ptr, length * (uint)sizeof(T), initial);

		/// <summary>
		/// Calculates and returns a deterministic hash code from <paramref name="ptr"/> to <paramref name="length"/>.
		/// The entire memory domain defined by the two parameters is scanned, and any change to it will alter the result.
		/// </summary>
		public static unsafe int GetHashCode(byte* ptr, uint length = 1, int initial = 0)
		{
			int hashCode = initial;

			unchecked
			{
				int* intPtr = (int*)ptr - 1;
				uint intLength = length / 4;

				for (uint i = 0; i < intLength; i++) hashCode = (hashCode * 397) ^ *++intPtr;
				for (uint i = intLength * 4; i < length; i++) hashCode = (hashCode * 397) ^ ptr[i];
			}

			return hashCode;
		}

		public static int  Morton(Int2 position) => Saw((short)position.x) | (Saw((short)position.y) << 1); //Uses Morton encoding to improve cache hit chance
		public static Int2 Morton(int  index)    => new(Unsaw(index), Unsaw(index >> 1));

		/// <summary>
		/// Transforms a number into a saw blade shape:
		/// _ _ _ _ _ _ _ _ 7 6 5 4 3 2 1 0
		/// _ 7 _ 6 _ 5 _ 4 _ 3 _ 2 _ 1 _ 0
		/// </summary>
		static int Saw(short number)
		{
			//NOTE: we can use the pext and pdep instructions under the BMI2 instruction set to accelerate this
			//https://stackoverflow.com/a/30540867/9196958

			int x = number;

			x = (x | (x << 08)) & 0b0000_0000_1111_1111_0000_0000_1111_1111; // _ _ _ _ 7 6 5 4 _ _ _ _ 3 2 1 0
			x = (x | (x << 04)) & 0b0000_1111_0000_1111_0000_1111_0000_1111; // _ _ 7 6 _ _ 5 4 _ _ 3 2 _ _ 1 0
			x = (x | (x << 02)) & 0b0011_0011_0011_0011_0011_0011_0011_0011; // _ 7 _ 6 _ 5 _ 4 _ 3 _ 2 _ 1 _ 0
			x = (x | (x << 01)) & 0b0101_0101_0101_0101_0101_0101_0101_0101; // Final step not representable in 8 bit version

			return x;
		}

		/// <summary>
		/// Transforms a saw blade shape number back:
		/// _ 7 _ 6 _ 5 _ 4 _ 3 _ 2 _ 1 _ 0
		/// _ _ _ _ _ _ _ _ 7 6 5 4 3 2 1 0
		/// </summary>
		static short Unsaw(int number)
		{
			int x = number;

			x = (x | (x >> 00)) & 0b0101_0101_0101_0101_0101_0101_0101_0101; // _ 7 _ 6 _ 5 _ 4 _ 3 _ 2 _ 1 _ 0
			x = (x | (x >> 01)) & 0b0011_0011_0011_0011_0011_0011_0011_0011; // _ _ 7 6 _ _ 5 4 _ _ 3 2 _ _ 1 0
			x = (x | (x >> 02)) & 0b0000_1111_0000_1111_0000_1111_0000_1111; // _ _ _ _ 7 6 5 4 _ _ _ _ 3 2 1 0
			x = (x | (x >> 04)) & 0b0000_0000_1111_1111_0000_0000_1111_1111; // _ _ _ _ _ _ _ _ 7 6 5 4 3 2 1 0

			return (short)(x | (x >> 08));
		}
	}
}