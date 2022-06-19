﻿using System.Runtime.Serialization.Json;
using System.Runtime.Intrinsics.X86;
using System;
using System.Linq;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;

namespace Echo.Core.Textures.Colors;

partial struct RGBA128
{
	readonly ref struct Parser
	{
		public Parser(ReadOnlySpan<char> span)
		{
			span = span.Trim();

			if (RemovePrefix(ref span, "0x"))
			{
				type = Type.hex;
			}
			else if (RemovePrefix(ref span, "#"))
			{
				type = Type.hex;
				RemovePrefix(ref span, "#");
			}
			else
			{
				if (!RemovePrefix(ref span, "rgb"))
				{
					RemovePrefix(ref span, "hdr");
					type = Type.hdr;
				}
				else type = Type.rgb;

				if (!RemoveParenthesis(ref span)) type = Type.error;
			}

			content = span;

			static bool RemovePrefix(ref ReadOnlySpan<char> span, ReadOnlySpan<char> prefix)
			{
				Assert.IsTrue(prefix.Length > 0);

				if (!span.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;

				span = span[prefix.Length..];
				return true;
			}

			static bool RemoveParenthesis(ref ReadOnlySpan<char> span)
			{
				if (span[0] != '(' || span[^1] != ')') return false;

				span = span[1..^1].Trim();
				return true;
			}
		}

		readonly Type type;
		readonly ReadOnlySpan<char> content;

		public bool Execute(out RGBA128 result) => type switch
		{
			Type.hex => ParseHex(out result),
			Type.rgb => ParseRGB(out result),
			Type.hdr => ParseHDR(out result),
			Type.error => YieldError(out result),
			_ => throw ExceptionHelper.Invalid(nameof(type), type, InvalidType.unexpected)
		};

		bool ParseHex(out RGBA128 result)
		{
			int r;
			int g;
			int b;
			int a = 255;

			switch (content.Length)
			{
				case 1:
					{
						if (ConvertHex(content[0], out int value))
						{
							r = g = b = CombineHex(value, value);
							break;
						}

						goto default;
					}
				case 3:
					{
						if (ConvertHex(content[0], out r) &&
							ConvertHex(content[1], out g) &&
							ConvertHex(content[2], out b))
						{
							r = CombineHex(r, r);
							g = CombineHex(g, g);
							b = CombineHex(b, b);
							break;
						}

						goto default;
					}
				case 4:
					{
						if (ConvertHex(content[3], out a))
						{
							a = CombineHex(a, a);
							goto case 3;
						}

						goto default;
					}
				case 6:
					{
						if (ConvertHex(content[0], out int r0) && ConvertHex(content[1], out int r1) &&
							ConvertHex(content[2], out int g0) && ConvertHex(content[3], out int g1) &&
							ConvertHex(content[4], out int b0) && ConvertHex(content[5], out int b1))
						{
							r = CombineHex(r0, r1);
							g = CombineHex(g0, g1);
							b = CombineHex(b0, b1);
							break;
						}

						goto default;
					}
				case 8:
					{
						if (ConvertHex(content[6], out int a0) && ConvertHex(content[7], out int a1))
						{
							a = CombineHex(a0, a1);
							goto case 6;
						}

						goto default;
					}
				default: return YieldError(out result);
			}

			return ConvertRGBA(r, g, b, a, out result);

			static int CombineHex(int hex0, int hex1) => hex0 * 16 + hex1;
		}

		bool ParseRGB(out RGBA128 result)
		{
			int index = 0;

			int r = 0, g = 0, b = 0, a = 255;

			if (findNextInt(content, ref index, ref r) &&
				findNextInt(content, ref index, ref g) &&
				findNextInt(content, ref index, ref b))
			{
				if (findNextInt(content, ref index, ref a)) return YieldError(out result);


				if (IsChannelInRange(r) &&
					IsChannelInRange(g) &&
					IsChannelInRange(b) &&
					IsChannelInRange(a))
				{
					result = new RGBA128(r / 255f, g / 255f, b / 255f, a / 255f);
					return true;
				}

			}
			return YieldError(out result);

			static bool IsChannelInRange(int channel) => channel > 0 && channel <= 255;
			static bool findNextInt(ReadOnlySpan<char> src, ref int index, ref int result)
			{
				int len = 0;

				while (!char.IsDigit(src[index])) if (src.Length <= index++) return false; // find first digit
				while (char.IsDigit(src[index + len]) && (index + len) < (src.Length - 1)) len++; // get the length of the number

				if (len == 0) return false;

				ReadOnlySpan<char> num = src.Slice(index, len);
				result = int.Parse(num);
				index += len;
				return true;
			}
		}

		bool ParseHDR(out RGBA128 result)
		{
			int index = 0;
			float r = 0f, g = 0f, b = 0f, a = 1f;

			if (findNextFloat(content, ref index, ref r) &&
				findNextFloat(content, ref index, ref g) &&
				findNextFloat(content, ref index, ref b))
			{
				if (findNextFloat(content, ref index, ref a)) return YieldError(out result);

				result = new RGBA128(r, g, b, a);
				return true;
			}
			return YieldError(out result);

			static bool findNextFloat(ReadOnlySpan<char> src, ref int index, ref float result)
			{
				int len = 0;

				while (!char.IsDigit(src[index])) if (src.Length <= index++) return false; // find first digit
				while ((char.IsDigit(src[index + len]) || src[index + len].Equals('.')) && (index + len) < (src.Length - 1)) len++; // get the length of the number

				if (len == 0) return false;

				ReadOnlySpan<char> num = src.Slice(index, len);
				float tempRes = 0f;
				bool parsed = float.TryParse(num, result: out tempRes);

				if (parsed) result = tempRes;
				else return false;
				index += len;

				return true;
			}
		}

		static bool ConvertRGBA(int r, int g, int b, int a, out RGBA128 result)
		{
			if (ConvertChannel(r, out float channel0) &&
				ConvertChannel(g, out float channel1) &&
				ConvertChannel(b, out float channel2) &&
				ConvertChannel(a, out float channel3))
			{
				result = new RGBA128(channel0, channel1, channel2, channel3);
				return true;
			}

			return YieldError(out result);

			static bool ConvertChannel(int value, out float result)
			{
				if ((uint)value >= 256)
				{
					result = 0f;
					return false;
				}

				result = value / 255f;
				return true;
			}
		}

		static bool ConvertHex(char digit, out int value)
		{
			value = digit switch
			{
				>= '0' and <= '9' => digit - '0',
				>= 'A' and <= 'F' => digit - 'A' + 10,
				>= 'a' and <= 'f' => digit - 'a' + 10,
				_ => -1
			};

			if (value >= 0) return true;

			value = 0;
			return false;
		}

		static bool YieldError(out RGBA128 result)
		{
			result = Black;
			return false;
		}

		enum Type : byte
		{
			hex,
			rgb,
			hdr,
			error
		}
	}
}