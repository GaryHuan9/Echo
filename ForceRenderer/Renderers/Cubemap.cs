using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using CodeHelpers;
using CodeHelpers.Vectors;

namespace ForceRenderer.Renderers
{
	public class Cubemap
	{
		public Cubemap(string path)
		{
			this.path = path;

			ReadOnlyCollection<string> names = IndividualMapNames;
			ReadOnlyCollection<Shade>[] sources = new ReadOnlyCollection<Shade>[names.Count];
			string[] extensions = {".png", ".jpg"};

			for (int i = 0; i < names.Count; i++)
			{
				string localPath = Path.Combine(path, names[i]);
				string fullPath;

				int extensionIndex = 0;

				do fullPath = Assets.GetAssetsPath(localPath, extensions[extensionIndex++]);
				while (!File.Exists(fullPath) && extensionIndex < extensions.Length);

				if (!File.Exists(fullPath)) throw ExceptionHelper.Invalid(nameof(path), path, $"has no image for {names[i]}");
				using var map = new Bitmap(fullPath);

				Int2 resolution = new Int2(map.Width, map.Height);

				if (resolution.x != resolution.y) throw ExceptionHelper.Invalid(nameof(path), localPath, "is not a square image!");
				if (mapSize != 0 && mapSize != resolution.x) throw ExceptionHelper.Invalid(nameof(path), localPath, "does not have the same size as other images!");

				mapSize = resolution.x;
				Shade[] pixels = new Shade[resolution.Product];

				for (int j = 0; j < pixels.Length; j++)
				{
					Int2 position = GetPixel(j);
					Color color = map.GetPixel(position.x, position.y);

					pixels[j] = new Shade(color.R, color.G, color.B);
				}

				sources[i] = new ReadOnlyCollection<Shade>(pixels);
			}

			maps = new ReadOnlyCollection<ReadOnlyCollection<Shade>>(sources);
		}

		public readonly string path;
		public readonly int mapSize;

		readonly ReadOnlyCollection<ReadOnlyCollection<Shade>> maps;
		public static readonly ReadOnlyCollection<string> IndividualMapNames = new ReadOnlyCollection<string>(new[] {"PositiveX", "PositiveY", "PositiveZ", "NegativeX", "NegativeY", "NegativeZ"});

		public Shade Sample(Float3 direction)
		{
			Float3 absoluted = direction.Absoluted;
			int index = absoluted.MaxIndex;

			float component = direction[index];
			if (direction[index] < 0f) index += 3;

			Float2 uv = index switch
						{
							0 => new Float2(-direction.z, direction.y),
							1 => new Float2(direction.x, -direction.z),
							2 => direction.XY,
							3 => direction.ZY,
							4 => direction.XZ,
							_ => new Float2(-direction.x, direction.y)
						};

			component = Math.Abs(component) * 2f;
			return Sample(index, uv / component);
		}

		/// <summary>
		/// Samples a specific bitmap at <paramref name="uv"/>.
		/// <paramref name="uv"/> is between -0.5 to 0.5 with zero in the middle.
		/// </summary>
		Shade Sample(int index, Float2 uv)
		{
			float x = (uv.x + 0.5f) * (mapSize - 1);
			float y = (0.5f - uv.y) * (mapSize - 1);

			return maps[index][GetIndex(new Int2(x.Round(), y.Round()))];
		}

		Int2 GetPixel(int index) => new Int2(index / mapSize, index % mapSize);
		int GetIndex(Int2 pixel) => pixel.x * mapSize + pixel.y;
	}
}