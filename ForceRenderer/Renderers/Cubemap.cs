using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using CodeHelpers.Vectors;

namespace ForceRenderer.Renderers
{
	public class Cubemap : IDisposable
	{
		public Cubemap(string path)
		{
			this.path = path;

			maps = new[]
				   {
					   Load("PositiveX"), Load("PositiveY"), Load("PositiveZ"),
					   Load("NegativeX"), Load("NegativeY"), Load("NegativeZ")
				   };

			Bitmap Load(string name)
			{
				string directory = Directory.GetParent(Environment.CurrentDirectory).Parent?.Parent?.FullName;
				string relative = Path.ChangeExtension(Path.Combine(path, name), ".png");

				return new Bitmap(Path.Combine(directory ?? "", relative));
			}
		}

		public readonly string path;
		readonly Bitmap[] maps;

		public Float3 Sample(Float3 direction)
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
		Float3 Sample(int index, Float2 uv)
		{
			Bitmap map = maps[index];

			float x = (uv.x + 0.5f) * (map.Width - 1);
			float y = (0.5f - uv.y) * (map.Height - 1);

			Color color = map.GetPixel(x.Round(), y.Round());
			return new Float3(color.R / 255f, color.G / 255f, color.B / 255f);
		}

		public void Dispose()
		{
			for (int i = 0; i < maps.Length; i++) maps[i].Dispose();
		}
	}
}