using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;
using ImageMagick;

namespace Echo.Core.InOut.Images;

public record MagickSerializer : Serializer
{
	MagickSerializer(MagickFormat format) => this.format = format;

	readonly MagickFormat format;

	public static readonly MagickSerializer png = new(MagickFormat.Png);
	public static readonly MagickSerializer jpeg = new(MagickFormat.Jpeg);
	public static readonly MagickSerializer tiff = new(MagickFormat.Tiff);
	public static readonly MagickSerializer exr = new(MagickFormat.Exr);
	public static readonly MagickSerializer hdr = new(MagickFormat.Hdr);

	public override unsafe void Serialize<T>(TextureGrid<T> texture, Stream stream)
	{
		Int2 size = texture.size;
		using var image = new MagickImage(MagickColors.None, size.X, size.Y);
		using IUnsafePixelCollection<float> pixels = image.GetPixelsUnsafe();
		float* pointer = (float*)pixels.GetAreaPointer(0, 0, size.X, size.Y);

		int channels = image.ChannelCount;
		Ensure.AreEqual(channels, 4);

		nint stride = size.X * channels;
		pointer += stride * (size.Y - 1);
		bool gamma = format is not (MagickFormat.Exr or MagickFormat.Hdr) && sRGB;

		for (int y = 0; y < size.Y; y++, pointer -= stride)
		for (int x = 0; x < size.X; x++)
		{
			Float4 color = texture[new Int2(x, y)].ToRGBA128();

			if (gamma) color = ColorConverter.LinearToGamma(Float4.Clamp(color));
			Sse.Store(pointer + x * channels, (color * ushort.MaxValue).v);
		}

		image.Write(stream, format);
	}

	public override unsafe SettableGrid<T> Deserialize<T>(Stream stream)
	{
		using var image = new MagickImage(stream);

		bool hasAlpha = image.Channels.Contains(PixelChannel.Alpha);
		bool hasBlack = image.Channels.Contains(PixelChannel.Black);

		if (hasBlack) throw new ArgumentException($"{nameof(MagickSerializer)} cannot deserialize CMYK images.", nameof(stream));

		Int2 size = new Int2(image.Width, image.Height);
		var texture = new ArrayGrid<T>(size);

		using IUnsafePixelCollection<float> pixels = image.GetPixelsUnsafe();
		float* pointer = (float*)pixels.GetAreaPointer(0, 0, size.X, size.Y);

		int channels = image.ChannelCount;
		nint stride = size.X * channels;
		pointer += stride * (size.Y - 1);
		float* border = pointer + stride;
			
		bool grayscale = channels < 3; //Gray scale images are not tested because I am too lazy to find one
		bool gamma = image.Format is not (MagickFormat.Exr or MagickFormat.Hdr) && sRGB;

		for (int y = 0; y < size.Y; y++, pointer -= stride)
		for (int x = 0; x < size.X; x++)
		{
			float* address = pointer + x * channels;
			Unsafe.SkipInit(out Float4 value);
			
			if (address + 4 < border) value = new Float4(Sse.LoadVector128(address));
			else Buffer.MemoryCopy(address, &value, sizeof(Float4), border - address);

			if (!hasAlpha)
			{
				value = ((RGBA128)value).AlphaOne;
				if (grayscale) value = value.XXXW;
			}
			else if (grayscale) value = value.XXXY;

			value = Float4.Max(Float4.Zero, value * (1f / ushort.MaxValue));
			if (!float.IsFinite(value.Sum)) value = Float4.One; //There might be degenerate pixels

			if (gamma) value = ColorConverter.GammaToLinear(value);
			texture.Set(new Int2(x, y), ((RGBA128)value).As<T>());
		}

		return texture;
	}
}