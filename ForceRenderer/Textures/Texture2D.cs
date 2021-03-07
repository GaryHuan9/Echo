using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.Intrinsics;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Files;
using CodeHelpers.Mathematics;
using ForceRenderer.IO;

namespace ForceRenderer.Textures
{
	/// <summary>
	/// The default texture; stores RGBA color information with 32 bits per channel.
	/// Supports saving and loading from image files.
	/// </summary>
	public class Texture2D : Texture, ILoadableAsset
	{
		public Texture2D(Int2 size) : base(size) => pixels = new Vector128<float>[size.Product];

		readonly Vector128<float>[] pixels;

		public override ref Vector128<float> this[int index] => ref pixels[index];

		static readonly ReadOnlyCollection<string> _acceptableFileExtensions = new(new[] {".png", ".jpg", ".tiff", ".bmp", ".gif", ".exif", FloatingPointImageExtension});
		static readonly ReadOnlyCollection<ImageFormat> compatibleFormats = new(new[] {ImageFormat.Png, ImageFormat.Jpeg, ImageFormat.Tiff, ImageFormat.Bmp, ImageFormat.Gif, ImageFormat.Exif, null});

		const string FloatingPointImageExtension = ".fpi";
		IReadOnlyList<string> ILoadableAsset.AcceptableFileExtensions => _acceptableFileExtensions;

		public void Save(string relativePath)
		{
			//Get path
			string extension = Path.GetExtension(relativePath);
			int extensionIndex;

			if (string.IsNullOrEmpty(extension))
			{
				extensionIndex = 0;
				relativePath = Path.ChangeExtension(relativePath, _acceptableFileExtensions[0]);
			}
			else
			{
				extensionIndex = _acceptableFileExtensions.IndexOf(extension);
				if (extensionIndex < 0) throw ExceptionHelper.Invalid(nameof(relativePath), relativePath, "does not have a compatible extension!");
			}

			string path = AssetsUtility.GetAssetsPath(relativePath);

			if (extension == FloatingPointImageExtension)
			{
				SaveFloatingPointImage(path);
				return;
			}

			//Export
			using Bitmap bitmap = new Bitmap(size.x, size.y);

			Rectangle rectangle = new Rectangle(0, 0, size.x, size.y);
			BitmapData bits = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

			unsafe
			{
				byte* pointer = (byte*)bits.Scan0;
				if (pointer == null) throw ExceptionHelper.NotPossible;

				for (int i = 0; i < length; i++)
				{
					Color32 color = (Color32)GetPixel(i);

					pointer[0] = color.b;
					pointer[1] = color.g;
					pointer[2] = color.r;
					pointer[3] = color.a;

					pointer += 4;
				}
			}

			bitmap.UnlockBits(bits);
			bitmap.Save(path, compatibleFormats[extensionIndex]);
		}

		public static Texture2D Load(string path)
		{
			path = ((Texture2D)white).GetAbsolutePath(path);

			if (Path.GetExtension(path) == FloatingPointImageExtension) return ReadFloatingPointImage(path);

			using Bitmap source = new Bitmap(path, true);
			PixelFormat format = source.PixelFormat;
			Int2 size = new Int2(source.Width, source.Height);

			Texture2D texture = new Texture2D(size);

			Rectangle rectangle = new Rectangle(0, 0, texture.size.x, texture.size.y);
			BitmapData data = source.LockBits(rectangle, ImageLockMode.ReadOnly, format);

			unsafe
			{
				byte* pointer = (byte*)data.Scan0;
				if (pointer == null) throw ExceptionHelper.NotPossible;

				switch (Image.GetPixelFormatSize(format))
				{
					case 24:
					{
						for (int i = 0; i < texture.length; i++)
						{
							texture.SetPixel(i, (Float4)new Color32(pointer[2], pointer[1], pointer[0]));
							pointer += 3;
						}

						break;
					}
					case 32:
					{
						for (int i = 0; i < texture.length; i++)
						{
							texture.SetPixel(i, (Float4)new Color32(pointer[2], pointer[1], pointer[0], pointer[3]));
							pointer += 4;
						}

						break;
					}
					default: throw ExceptionHelper.Invalid(nameof(format), format, "is not an acceptable format!");
				}
			}

			source.UnlockBits(data);
			return texture;
		}

		void SaveFloatingPointImage(string path)
		{
			using DataWriter writer = new DataWriter(new GZipStream(File.OpenWrite(path), CompressionLevel.Optimal));

			writer.Write(0); //Writes version number
			Write(writer);
		}

		static Texture2D ReadFloatingPointImage(string path)
		{
			using DataReader reader = new DataReader(new GZipStream(File.OpenRead(path), CompressionMode.Decompress));

			reader.ReadInt32(); //Reads version number
			return Read(reader);
		}

		public void Write(DataWriter writer)
		{
			writer.Write(size);
			for (int i = 0; i < length; i++) writer.Write(GetPixel(i));
		}

		public static Texture2D Read(DataReader reader)
		{
			Int2 size = reader.ReadInt2();
			Texture2D texture = new Texture2D(size);

			for (int i = 0; i < texture.length; i++) texture.SetPixel(i, reader.ReadFloat4());

			return texture;
		}

		public override void CopyFrom(Texture texture)
		{
			if (texture is not Texture2D texture2D) base.CopyFrom(texture);
			else Array.Copy(texture2D.pixels, pixels, length);
		}

		unsafe ref Float4 GetPixel(int index)
		{
			var data = this[index];
			return ref *(Float4*)&data;
		}

		unsafe void SetPixel(int index, Float4 pixel) => this[index] = *(Vector128<float>*)&pixel;
	}
}