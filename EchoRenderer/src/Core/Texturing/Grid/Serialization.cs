using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Files;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common;
using EchoRenderer.InOut;

namespace EchoRenderer.Core.Texturing.Grid;

public partial class TextureGrid
{
	static readonly ReadOnlyCollection<string> acceptableFileExtensions = new(new[] { ".png", ".jpg", ".tiff", ".bmp", ".gif", ".exif", FloatingPointImageExtension });
	static readonly ReadOnlyCollection<ImageFormat> compatibleFormats = new(new[] { ImageFormat.Png, ImageFormat.Jpeg, ImageFormat.Tiff, ImageFormat.Bmp, ImageFormat.Gif, ImageFormat.Exif, null });

	const string FloatingPointImageExtension = ".fpi";

	const float GammaThreshold = 0.0031308f;
	const float GammaMultiplier = 12.92f;
	const float GammaOffset = 0.055f;
	const float GammaExponent = 2.4f;

	public Task SaveAsync(string relativePath, bool sRGB = true) => Task.Run(() => Save(relativePath, sRGB));

	public void Save(string relativePath, bool sRGB = true)
	{
		//Get path
		string extension = Path.GetExtension(relativePath);
		int extensionIndex;

		if (string.IsNullOrEmpty(extension))
		{
			extensionIndex = 0;
			relativePath = Path.ChangeExtension(relativePath, acceptableFileExtensions[0]);
		}
		else
		{
			extensionIndex = acceptableFileExtensions.IndexOf(extension);
			if (extensionIndex < 0) throw ExceptionHelper.Invalid(nameof(relativePath), relativePath, "does not have a compatible extension!");
		}

		string path = AssetsUtility.GetAssetsPath(relativePath);

		if (extension == FloatingPointImageExtension)
		{
			SaveFloatingPointImage(path);
			return;
		}

		//Export
		using Bitmap bitmap = new Bitmap(size.X, size.Y);

		Rectangle rectangle = new Rectangle(0, 0, size.X, size.Y);
		BitmapData bits = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

		unsafe
		{
			byte* origin = (byte*)bits.Scan0;

			ForEach(SaveARGB);

			void SaveARGB(Int2 position)
			{
				Vector128<float> colorVector = this[position];
				byte* pointer = origin + ToPointerOffset(position) * 4;

				if (sRGB) colorVector = ForwardGammaCorrect(colorVector);
				Color32 color = (Color32)Utilities.ToFloat4(colorVector);

				pointer[0] = color.b;
				pointer[1] = color.g;
				pointer[2] = color.r;
				pointer[3] = color.a;
			}
		}

		bitmap.UnlockBits(bits);
		bitmap.Save(path, compatibleFormats[extensionIndex]);
	}

	public static Task<ArrayGrid> LoadAsync(string path, bool sRGB = true) => Task.Run(() => Load(path, sRGB));

	public static unsafe ArrayGrid Load(string path, bool sRGB = true)
	{
		path = AssetsUtility.GetAbsolutePath(acceptableFileExtensions, path);

		if (Path.GetExtension(path) == FloatingPointImageExtension) return ReadFloatingPointImage(path);

		using Bitmap source = new Bitmap(path, true);
		PixelFormat format = source.PixelFormat;
		Int2 size = new Int2(source.Width, source.Height);

		ArrayGrid texture = new ArrayGrid(size);

		Rectangle rectangle = new Rectangle(0, 0, texture.size.X, texture.size.Y);
		BitmapData data = source.LockBits(rectangle, ImageLockMode.ReadOnly, format);

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

		void LoadRGB(Int2 position)
		{
			byte* pointer = origin + texture.ToPointerOffset(position) * 3;

			Color32 pixel = new Color32(pointer[2], pointer[1], pointer[0]);
			Vector128<float> vector = Utilities.ToVector((Float4)pixel);

			texture[position] = sRGB ? InverseGammaCorrect(vector) : vector;
		}

		void LoadARGB(Int2 position)
		{
			byte* pointer = origin + texture.ToPointerOffset(position) * 4;

			Color32 pixel = new Color32(pointer[2], pointer[1], pointer[0], pointer[3]);
			Vector128<float> vector = Utilities.ToVector((Float4)pixel);

			texture[position] = sRGB ? InverseGammaCorrect(vector) : vector;
		}

		source.UnlockBits(data);
		return texture;
	}

	/// <summary>
	/// Returns an index/offset to the origin of a <see cref="BitmapData"/> during serialization.
	/// </summary>
	int ToPointerOffset(Int2 position) => position.X + (oneLess.Y - position.Y) * size.X;

	static unsafe Vector128<float> ForwardGammaCorrect(Vector128<float> value)
	{
		float* pointer = (float*)&value;

		for (int i = 0; i < 4; i++) Operate(pointer + i);

		return value;

		static void Operate(float* target)
		{
			if (*target <= GammaThreshold) *target *= GammaMultiplier;
			else *target = (1f + GammaOffset) * MathF.Pow(*target, 1f / GammaExponent) - GammaOffset;
		}
	}

	static unsafe Vector128<float> InverseGammaCorrect(Vector128<float> value)
	{
		float* pointer = (float*)&value;

		for (int i = 0; i < 4; i++) Operate(pointer + i);

		return value;

		static void Operate(float* target)
		{
			if (*target <= GammaThreshold * GammaMultiplier) *target *= 1f / GammaMultiplier;
			else *target = MathF.Pow((*target + GammaOffset) * (1f / (1f + GammaOffset)), GammaExponent);
		}
	}

	//Fast gamma correction (gamma = 2.0)
	// static Vector128<float> ForwardGammaCorrect(Vector128<float> value) => Sse.Sqrt(value);
	// static Vector128<float> InverseGammaCorrect(Vector128<float> value) => Sse.Multiply(value, value);

	void SaveFloatingPointImage(string path)
	{
		using DataWriter writer = new DataWriter(File.Open(path, FileMode.Create));

		writer.Write(1); //Writes version number
		Write(writer);
	}

	static ArrayGrid ReadFloatingPointImage(string path)
	{
		using DataReader reader = new DataReader(File.Open(path, FileMode.Open));

		int version = reader.ReadInt32(); //Reads version number
		if (version == 0) throw new NotSupportedException();

		return Read(reader);
	}

	unsafe void Write(DataWriter writer)
	{
		writer.WriteCompact(size);
		var sequence = Vector128<uint>.Zero;

		foreach (Int2 position in size.Loop())
		{
			Vector128<uint> current = this[position].AsUInt32();
			Vector128<uint> xor = Sse2.Xor(sequence, current);

			//Write the xor difference as variable length quantity for lossless compression

			sequence = current;
			uint* pointer = (uint*)&xor;

			for (int i = 0; i < 4; i++) writer.WriteCompact(pointer[i]);
		}
	}

	static unsafe ArrayGrid Read(DataReader reader)
	{
		Int2 size = reader.ReadInt2Compact();
		ArrayGrid texture = new ArrayGrid(size);

		var sequence = Vector128<uint>.Zero;
		uint* read = stackalloc uint[4];

		//Read the xor difference sequence

		foreach (Int2 position in size.Loop())
		{
			for (int i = 0; i < 4; i++) read[i] = reader.ReadUInt32Compact();

			Vector128<uint> xor = *(Vector128<uint>*)read;
			Vector128<uint> current = Sse2.Xor(sequence, xor);

			texture[position] = current.AsSingle();
			sequence = current;
		}

		return texture;
	}
}