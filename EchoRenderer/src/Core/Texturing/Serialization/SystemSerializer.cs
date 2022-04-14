﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using CodeHelpers;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Coloring;
using EchoRenderer.Core.Texturing.Grid;

namespace EchoRenderer.Core.Texturing.Serialization;

/// <summary>
/// An <see cref="ISerializer"/> implemented through classes from the <see cref="System.Drawing.Imaging"/> namespace.
/// </summary>
public record SystemSerializer(ImageFormat Format, bool sRGB = true) : ISerializer
{
	public static readonly SystemSerializer png = new(ImageFormat.Png);
	public static readonly SystemSerializer jpeg = new(ImageFormat.Jpeg);
	public static readonly SystemSerializer tiff = new(ImageFormat.Tiff);
	public static readonly SystemSerializer bmp = new(ImageFormat.Bmp);
	public static readonly SystemSerializer gif = new(ImageFormat.Gif);
	public static readonly SystemSerializer exif = new(ImageFormat.Exif);

	const float GammaThreshold = 0.0031308f;
	const float GammaMultiplier = 12.92f;
	const float GammaOffset = 0.055f;
	const float GammaExponent = 2.4f;

	/// <inheritdoc/>
	public unsafe void Serialize<T>(TextureGrid<T> texture, Stream stream) where T : IColor<T>
	{
		Int2 size = texture.size;

		using var bitmap = new Bitmap(size.X, size.Y);
		var rectangle = new Rectangle(0, 0, size.X, size.Y);

		BitmapData bits = bitmap.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

		byte* origin = (byte*)bits.Scan0;

		texture.ForEach(SaveARGB);
		bitmap.UnlockBits(bits);
		bitmap.Save(stream, Format);

		void SaveARGB(Int2 position)
		{
			RGBA128 source = texture[position].ToRGBA128();
			byte* pointer = origin + Offset(position) * 4;

			if (sRGB) ForwardGammaCorrect(ref source);
			var color = (Color32)(Float4)source;

			pointer[0] = color.b;
			pointer[1] = color.g;
			pointer[2] = color.r;
			pointer[3] = color.a;
		}

		int Offset(Int2 position) => position.X + (texture.oneLess.Y - position.Y) * size.X;
	}

	/// <inheritdoc/>
	public unsafe TextureGrid<T> Deserialize<T>(Stream stream) where T : IColor<T>
	{
		using var bitmap = new Bitmap(stream, true);
		PixelFormat format = bitmap.PixelFormat;

		var size = new Int2(bitmap.Width, bitmap.Height);
		var rectangle = new Rectangle(0, 0, size.X, size.Y);

		BitmapData data = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, format);

		var texture = new ArrayGrid<T>(size);
		byte* origin = (byte*)data.Scan0;

		switch (Image.GetPixelFormatSize(format))
		{
			case 24:
			{
				texture.ForEach(LoadRGB);
				break;
			}
			case 32:
			{
				texture.ForEach(LoadARGB);
				break;
			}
			default: throw ExceptionHelper.Invalid(nameof(format), format, "is not an acceptable format!");
		}

		bitmap.UnlockBits(data);
		return texture;

		void LoadRGB(Int2 position)
		{
			byte* ptr = origin + Offset(position) * 3;
			var color = new Color32(ptr[2], ptr[1], ptr[0]);

			var source = (RGBA128)(Float4)color;
			if (sRGB) InverseGammaCorrect(ref source);

			texture[position] = source.As<T>();
		}

		void LoadARGB(Int2 position)
		{
			byte* ptr = origin + Offset(position) * 4;
			var color = new Color32(ptr[2], ptr[1], ptr[0], ptr[3]);

			var source = (RGBA128)(Float4)color;
			if (sRGB) InverseGammaCorrect(ref source);

			texture[position] = source.As<T>();
		}

		int Offset(Int2 position) => position.X + (texture.oneLess.Y - position.Y) * size.X;
	}

	static void ForwardGammaCorrect(ref RGBA128 value)
	{
		ref float reference = ref Unsafe.As<RGBA128, float>(ref value);
		for (int i = 0; i < 3; i++) Operate(ref Unsafe.Add(ref reference, i));

		static void Operate(ref float target)
		{
			if (target <= GammaThreshold) target *= GammaMultiplier;
			else target = (1f + GammaOffset) * MathF.Pow(target, 1f / GammaExponent) - GammaOffset;
		}
	}

	static void InverseGammaCorrect(ref RGBA128 value)
	{
		ref float reference = ref Unsafe.As<RGBA128, float>(ref value);
		for (int i = 0; i < 3; i++) Operate(ref Unsafe.Add(ref reference, i));

		static void Operate(ref float target)
		{
			if (target <= GammaThreshold * GammaMultiplier) target *= 1f / GammaMultiplier;
			else target = MathF.Pow((target + GammaOffset) * (1f / (1f + GammaOffset)), GammaExponent);
		}
	}

	//Fast gamma correction (gamma = 2.0)
	// static Vector128<float> ForwardGammaCorrect(Vector128<float> value) => Sse.Sqrt(value);
	// static Vector128<float> InverseGammaCorrect(Vector128<float> value) => Sse.Multiply(value, value);
}