using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Vectors;

namespace ForceRenderer.IO
{
	/// <summary>
	/// An asset object used to read or save an image. Pixels are stored raw for fast access but uses much more memory.
	/// File operation handled by <see cref="Bitmap"/>. Can be offloaded to separate threads for faster loading.
	/// </summary>
	public class Texture : LoadableAsset
	{
		public Texture(string path)
		{
			using Bitmap source = new Bitmap(GetAbsolutePath(path), true);
			PixelFormat format = source.PixelFormat;

			size = new Int2(source.Width, source.Height);
			isReadonly = true;

			oneLess = size - Int2.one;
			aspect = (float)size.x / size.y;
			length = size.Product;

			pixels = new Color32[length];

			Rectangle rectangle = new Rectangle(0, 0, size.x, size.y);
			BitmapData data = source.LockBits(rectangle, ImageLockMode.ReadOnly, format);

			unsafe
			{
				byte* pointer = (byte*)data.Scan0;
				if (pointer == null) throw ExceptionHelper.NotPossible;

				switch (Image.GetPixelFormatSize(format))
				{
					case 24:
					{
						for (int i = 0; i < length; i++)
						{
							pixels[i] = new Color32(pointer[2], pointer[1], pointer[0]);
							pointer += 3;
						}

						break;
					}
					case 32:
					{
						for (int i = 0; i < length; i++)
						{
							pixels[i] = new Color32(pointer[2], pointer[1], pointer[0], pointer[3]);
							pointer += 4;
						}

						break;
					}
					default: throw ExceptionHelper.Invalid(nameof(format), format, "is not an acceptable format!");
				}
			}

			source.UnlockBits(data);
		}

		public Texture(Texture source, bool isReadonly = false) : this(source.size, (Color32[])source.pixels.Clone(), isReadonly) { }

		public Texture(Int2 size, bool isReadonly = false) : this(size, new Color32[size.Product], isReadonly) { }

		Texture(Int2 size, Color32[] pixels, bool isReadonly)
		{
			this.size = size;
			this.isReadonly = isReadonly;

			oneLess = size - Int2.one;
			aspect = (float)size.x / size.y;
			length = size.Product;

			if (pixels.Length == length) this.pixels = pixels;
			else throw ExceptionHelper.Invalid(nameof(pixels), pixels, "has mismatched length.");
		}

		readonly Color32[] pixels;

		public readonly Int2 size;
		public readonly bool isReadonly;

		readonly Int2 oneLess;
		public readonly float aspect; //Width over height
		public readonly int length;

		static readonly ReadOnlyCollection<string> _acceptableFileExtensions = new ReadOnlyCollection<string>(new[] {".png", ".jpg", ".tiff", ".bmp", ".gif", ".exif"});
		static readonly ReadOnlyCollection<ImageFormat> compatibleFormats = new ReadOnlyCollection<ImageFormat>(new[] {ImageFormat.Png, ImageFormat.Jpeg, ImageFormat.Tiff, ImageFormat.Bmp, ImageFormat.Gif, ImageFormat.Exif});

		protected override IReadOnlyList<string> AcceptableFileExtensions => _acceptableFileExtensions;

		public static readonly Texture white = new Texture(Int2.one, new[] {Color32.white}, true);
		public static readonly Texture black = new Texture(Int2.one, new[] {Color32.black}, true);

		public Color32 GetPixel(int index) => pixels[index];
		public Color32 GetPixel(Int2 position) => GetPixel(ToIndex(position));
		public Color32 GetPixel(Float2 uv) => GetPixel(ToIndex(uv));

		public void SetPixel(int index, Color32 value)
		{
			if (!isReadonly) pixels[index] = value;
			else throw new ReadOnlyException();
		}

		public void SetPixel(Int2 position, Color32 value) => SetPixel(ToIndex(position), value);
		public void SetPixel(Float2 uv, Color32 value) => SetPixel(ToIndex(uv), value);

		public Int2 ToPosition(Float2 uv) => (uv * size).Floored.Clamp(Int2.zero, oneLess);
		public Int2 ToPosition(int index) => new Int2(index % size.x, oneLess.y - index / size.x);

		public int ToIndex(Int2 position) => position.x + (oneLess.y - position.y) * size.x;
		public int ToIndex(Float2 uv) => ToIndex(ToPosition(uv));

		public Float2 ToUV(Int2 position) => (position + Float2.half) / size;
		public Float2 ToUV(Float2 position) => position / size;
		public Float2 ToUV(int index) => ToUV(ToPosition(index));

		public void SaveFile(string relativePath)
		{
			//Get path
			string extension = Path.GetExtension(relativePath);
			int extensionIndex;

			if (string.IsNullOrEmpty(extension))
			{
				extensionIndex = 0;
				relativePath = Path.ChangeExtension(relativePath, AcceptableFileExtensions[extensionIndex]);
			}
			else
			{
				extensionIndex = AcceptableFileExtensions.IndexOf(extension);
				if (extensionIndex < 0) throw ExceptionHelper.Invalid(nameof(relativePath), relativePath, "does not have a compatible extension!");
			}

			string path = AssetsUtility.GetAssetsPath(relativePath);

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
					Color32 pixel = pixels[i];

					pointer[0] = pixel.b;
					pointer[1] = pixel.g;
					pointer[2] = pixel.r;
					pointer[3] = pixel.a;

					pointer += 4;
				}
			}

			bitmap.UnlockBits(bits);
			bitmap.Save(path, compatibleFormats[extensionIndex]);
		}
	}
}