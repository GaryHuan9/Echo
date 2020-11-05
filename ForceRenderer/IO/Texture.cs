using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using CodeHelpers;
using CodeHelpers.Vectors;

namespace ForceRenderer.IO
{
	/// <summary>
	/// An asset object used to read or save an image. Pixels are stored raw for fast access but uses much more memory.
	/// File operation handled by <see cref="Bitmap"/>. Can be offloaded to separate threads for faster loading.
	/// </summary>
	public class Texture
	{
		public Texture(string relativePath)
		{
			string extension = Path.GetExtension(relativePath);
			string path = null;

			if (string.IsNullOrEmpty(extension))
			{
				//No provided extension, check through all available extensions
				string assetPath = AssetsUtility.GetAssetsPath(relativePath);

				foreach (string compatibleExtension in compatibleExtensions)
				{
					path = Path.ChangeExtension(assetPath, compatibleExtension);
					if (File.Exists(path)) break;
				}
			}
			else
			{
				if (compatibleExtensions.Contains(extension)) path = AssetsUtility.GetAssetsPath(relativePath);
				else throw new FileNotFoundException($"Incompatible file type at {relativePath} for {nameof(Texture)}");
			}

			if (string.IsNullOrEmpty(path) || !File.Exists(path)) throw new FileNotFoundException($"No file found at {path} for {nameof(Texture)}");

			//Load texture
			using Bitmap source = new Bitmap(path, true);
			PixelFormat format = source.PixelFormat;

			size = new Int2(source.Width, source.Height);
			oneLess = size - Int2.one;
			aspect = (float)size.x / size.y;

			length = size.Product;
			pixels = new Shade[length];

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
							pixels[i] = new Shade(pointer[2], pointer[1], pointer[0]);
							pointer += 3;
						}

						break;
					}
					case 32:
					{
						for (int i = 0; i < length; i++)
						{
							pixels[i] = new Shade(pointer[2], pointer[1], pointer[0], pointer[3]);
							pointer += 4;
						}

						break;
					}
					default: throw ExceptionHelper.Invalid(nameof(format), format, "is not an acceptable format!");
				}
			}

			source.UnlockBits(data);
		}

		public Texture(Int2 size)
		{
			this.size = size;
			oneLess = size - Int2.one;
			aspect = (float)size.x / size.y;

			length = size.Product;
			pixels = new Shade[length];
		}

		readonly Shade[] pixels;

		public readonly Int2 size;
		readonly Int2 oneLess;

		public readonly float aspect; //Width over height
		public readonly int length;

		public static readonly ReadOnlyCollection<string> compatibleExtensions = new ReadOnlyCollection<string>(new[] {".png", ".jpg", ".tiff", ".bmp", ".gif", ".exif"});
		static readonly ReadOnlyCollection<ImageFormat> compatibleFormats = new ReadOnlyCollection<ImageFormat>(new[] {ImageFormat.Png, ImageFormat.Jpeg, ImageFormat.Tiff, ImageFormat.Bmp, ImageFormat.Gif, ImageFormat.Exif});

		public Shade GetPixel(int index) => pixels[index];
		public Shade GetPixel(Int2 position) => GetPixel(ToIndex(position));
		public Shade GetPixel(Float2 uv) => GetPixel(ToIndex(uv));

		public void SetPixel(int index, Shade value) => pixels[index] = value;
		public void SetPixel(Int2 position, Shade value) => SetPixel(ToIndex(position), value);
		public void SetPixel(Float2 uv, Shade value) => SetPixel(ToIndex(uv), value);

		public void SaveFile(string relativePath)
		{
			//Get path
			string extension = Path.GetExtension(relativePath);
			int extensionIndex;

			if (string.IsNullOrEmpty(extension))
			{
				extensionIndex = 0;
				relativePath = Path.ChangeExtension(relativePath, compatibleExtensions[extensionIndex]);
			}
			else
			{
				extensionIndex = compatibleExtensions.IndexOf(extension);
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
					Shade pixel = pixels[i];

					pointer[0] = pixel.B;
					pointer[1] = pixel.G;
					pointer[2] = pixel.R;
					pointer[3] = pixel.A;

					pointer += 4;
				}
			}

			bitmap.UnlockBits(bits);
			bitmap.Save(path, compatibleFormats[extensionIndex]);
		}

		public Int2 ToPosition(Float2 uv) => (uv * size).Floored.Clamp(Int2.zero, oneLess);
		public Int2 ToPosition(int index) => new Int2(index % size.x, oneLess.y - index / size.x);

		public int ToIndex(Int2 position) => position.x + (oneLess.y - position.y) * size.x;
		public int ToIndex(Float2 uv) => ToIndex(ToPosition(uv));

		public Float2 ToUV(Int2 position) => (position + Float2.half) / size;
		public Float2 ToUV(Float2 position) => position / size;
		public Float2 ToUV(int index) => ToUV(ToPosition(index));
	}
}