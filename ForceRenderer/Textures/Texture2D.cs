using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Files;
using CodeHelpers.Mathematics;
using ForceRenderer.IO;

namespace ForceRenderer.Textures
{
	/// <summary>
	/// The default texture; stores RGBA color information with 8 bits per channel.
	/// Supports saving and loading from image files.
	/// </summary>
	public class Texture2D : Texture, ILoadableAsset
	{
		public Texture2D(Int2 size, IWrapper wrapper = null, IFilter filter = null) : base(size, wrapper, filter) => pixels = new Color32[size.Product];

		public Texture2D(Texture source, IWrapper wrapper = null, IFilter filter = null) : this(source.size, wrapper, filter)
		{
			if (source is Texture2D texture)
			{
				for (int i = 0; i < length; i++) pixels[i] = texture.pixels[i];
			}
			else CopyFrom(source);
		}

		Texture2D(Color32 color) : base(Int2.one) => pixels = new[] {color};

		readonly Color32[] pixels;

		public static readonly Texture2D white = new Texture2D(Color32.white);
		public static readonly Texture2D black = new Texture2D(Color32.black);

		public override Float3 this[int index]
		{
			get => (Float3)pixels[index];
			set
			{
				CheckReadonly();
				pixels[index] = (Color32)value;
			}
		}

		static readonly ReadOnlyCollection<string> _acceptableFileExtensions = new ReadOnlyCollection<string>(new[] {".png", ".jpg", ".tiff", ".bmp", ".gif", ".exif"});
		static readonly ReadOnlyCollection<ImageFormat> compatibleFormats = new ReadOnlyCollection<ImageFormat>(new[] {ImageFormat.Png, ImageFormat.Jpeg, ImageFormat.Tiff, ImageFormat.Bmp, ImageFormat.Gif, ImageFormat.Exif});

		IReadOnlyList<string> ILoadableAsset.AcceptableFileExtensions => _acceptableFileExtensions;

		public void Save(string relativePath)
		{
			//Get path
			string extension = Path.GetExtension(relativePath);
			int extensionIndex;

			if (string.IsNullOrEmpty(extension))
			{
				extensionIndex = 0;
				relativePath = Path.ChangeExtension(relativePath, ((ILoadableAsset)this).AcceptableFileExtensions[extensionIndex]);
			}
			else
			{
				extensionIndex = ((ILoadableAsset)this).AcceptableFileExtensions.IndexOf(extension);
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

		public static Texture2D Load(string path, IWrapper wrapper = null, IFilter filter = null)
		{
			using Bitmap source = new Bitmap(white.GetAbsolutePath(path), true);

			PixelFormat format = source.PixelFormat;
			Int2 size = new Int2(source.Width, source.Height);

			Texture2D texture = new Texture2D(size, wrapper, filter);
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
			texture.SetReadonly();

			return texture;
		}

		public void Write(FileWriter writer)
		{
			writer.Write(size);
			for (int i = 0; i < length; i++) writer.Write(pixels[i]);
		}

		public static Texture2D Read(FileReader reader)
		{
			Int2 size = reader.ReadInt2();
			Texture2D texture = new Texture2D(size);

			for (int i = 0; i < texture.length; i++) texture.pixels[i] = reader.ReadColor32();

			texture.SetReadonly();
			return texture;
		}
	}
}