using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Mathematics;

namespace ForceRenderer.IO
{
	/// <summary>
	/// The default texture; stores RGBA color information with 8 bits per channel.
	/// Supports saving and loading from image files.
	/// </summary>
	public class Texture2D : Texture
	{
		protected Texture2D(Color32[] pixels, Int2 size, bool isReadonly = false) : base(size, isReadonly)
		{
			if (pixels.Length == length) this.pixels = pixels;
			else throw ExceptionHelper.Invalid(nameof(pixels), pixels, "has mismatched length.");
		}

		public Texture2D(Int2 size, bool isReadonly = false) : base(size, isReadonly) => pixels = new Color32[length];

		readonly Color32[] pixels;

		public static readonly Texture2D white = new Texture2D(new[] {Color32.white}, Int2.one, true);
		public static readonly Texture2D black = new Texture2D(new[] {Color32.black}, Int2.one, true);

		public override Float3 this[int index]
		{
			get => (Float3)pixels[index];
			set
			{
				CheckReadonly();
				pixels[index] = (Color32)value;
			}
		}

		public override Texture Clone(bool newReadonly = false) => new Texture2D((Color32[])pixels.Clone(), size, newReadonly);

		public static Texture2D Load(string path)
		{
			using Bitmap source = new Bitmap(white.GetAbsolutePath(path), true);

			PixelFormat format = source.PixelFormat;
			Int2 size = new Int2(source.Width, source.Height);

			Texture2D texture = new Texture2D(size, true);
			Color32[] pixels = texture.pixels;

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
						for (int i = 0; i < pixels.Length; i++)
						{
							pixels[i] = new Color32(pointer[2], pointer[1], pointer[0]);
							pointer += 3;
						}

						break;
					}
					case 32:
					{
						for (int i = 0; i < pixels.Length; i++)
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
			return texture;
		}

		public void Save(string relativePath)
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