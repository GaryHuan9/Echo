using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Mathematics
{
	public static class Utilities
	{
		public static readonly Float3 luminanceOption = new Float3(0.2126f, 0.7152f, 0.0722f);
		public static readonly Vector128<float> luminanceVector = Vector128.Create(0.2126f, 0.7152f, 0.0722f, 0f);

		public static Float4 ToFloat4(Vector128<float> pixel) => Unsafe.As<Vector128<float>, Float4>(ref pixel);
		public static Float3 ToFloat3(Vector128<float> pixel) => Unsafe.As<Vector128<float>, Float3>(ref pixel);

		public static Vector128<float> ToVector(Float4 pixel) => Unsafe.As<Float4, Vector128<float>>(ref pixel);
		public static Vector128<float> ToVector(Float3 pixel) => Unsafe.As<Float3, Vector128<float>>(ref pixel);

		public static Float4 ToColor(float value) => new Float4(value, value, value, 1f);
		public static Float4 ToColor(Float3 value) => new Float4(value.x, value.y, value.z, 1f);

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector128<float> Lerp(in Vector128<float> left, in Vector128<float> right, in Vector128<float> time)
		{
			Vector128<float> length = Sse.Subtract(right, left);

			if (Fma.IsSupported) return Fma.MultiplyAdd(length, time, left);
			return Sse.Add(Sse.Multiply(length, time), left);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe float GetLuminance(in Vector128<float> color)
		{
			var vector = Sse41.DotProduct(color, luminanceVector, 0b1110_0001);
			return *(float*)&vector;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float GetLuminance(in Float3 color) => color.Dot(luminanceOption);

		public static int Morton(Int2 position) => Saw((short)position.x) | (Saw((short)position.y) << 1); //Uses Morton encoding to improve cache hit chance
		public static Int2 Morton(int index) => new Int2(Unsaw(index), Unsaw(index >> 1));

		/// <summary>
		/// Transforms a number into a saw blade shape:
		/// _ _ _ _ _ _ _ _ 7 6 5 4 3 2 1 0
		/// _ 7 _ 6 _ 5 _ 4 _ 3 _ 2 _ 1 _ 0
		/// </summary>
		static int Saw(short number)
		{
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