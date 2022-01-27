using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using CodeHelpers;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Common
{
	public static class Utilities
	{
		public static ref readonly Float4 ToFloat4(in Vector128<float> pixel) => ref Unsafe.As<Vector128<float>, Float4>(ref Unsafe.AsRef(pixel));
		public static ref readonly Float3 ToFloat3(in Vector128<float> pixel) => ref Unsafe.As<Vector128<float>, Float3>(ref Unsafe.AsRef(pixel));
		public static ref readonly Vector128<float> ToVector(in Float4 value) => ref Unsafe.As<Float4, Vector128<float>>(ref Unsafe.AsRef(value));

		//NOTE: to vector with ref values is unsafe and not provided for float3 since we can access weird memory with it.
		public static Vector128<float> ToVector(Float3 value) => Unsafe.As<Float3, Vector128<float>>(ref value);

		public static Float4 ToColor(float value) => ToColor((Float4)value);
		public static Float4 ToColor(in Float3 value) => ToColor((Float4)value);
		public static Float4 ToColor(in Float4 value) => value.Replace(3, 1f);

		public static Float4 ToColor(ReadOnlySpan<char> value)
		{
			value = value.Trim();
			value = value.Trim('#');
			if (value.StartsWith("0x")) value = value[2..];

			return value.Length switch
			{
				3 => (Float4)new Color32(ParseOne(value[0]), ParseOne(value[1]), ParseOne(value[2])),
				4 => (Float4)new Color32(ParseOne(value[0]), ParseOne(value[1]), ParseOne(value[2]), ParseOne(value[3])),
				6 => (Float4)new Color32(ParseTwo(value[..2]), ParseTwo(value[2..4]), ParseTwo(value[4..])),
				8 => (Float4)new Color32(ParseTwo(value[..2]), ParseTwo(value[2..4]), ParseTwo(value[4..6]), ParseTwo(value[6..])),
				_ => throw ExceptionHelper.Invalid(nameof(value), value.ToString(), "is not valid hex")
			};

			static byte ParseOne(char value)
			{
				int hex = ParseHex(value);
				return (byte)(hex * 16 + hex);
			}

			static byte ParseTwo(ReadOnlySpan<char> value) => (byte)(ParseHex(value[0]) * 16 + ParseHex(value[1]));

			static int ParseHex(char value) => value switch
			{
				>= '0' and <= '9' => value - '0',
				>= 'A' and <= 'F' => value - 'A' + 10,
				>= 'a' and <= 'f' => value - 'a' + 10,
				_ => throw ExceptionHelper.Invalid(nameof(value), value, "is not valid hex")
			};
		}

		/// <summary>
		/// Returns whether we should consider <paramref name="radiance"/> as positive.
		/// NOTE: this is only a temporary solution, we should move to a property after
		/// Spectrum or some thing for color is introduced.
		/// </summary>
		public static bool PositiveRadiance(this in Float3 radiance) => !(radiance < Constants.radianceEpsilon);

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

		public static int Morton(Int2 position) => Saw((short)position.x) | (Saw((short)position.y) << 1); //Uses Morton encoding to improve cache hit chance
		public static Int2 Morton(int index) => new(Unsaw(index), Unsaw(index >> 1));

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