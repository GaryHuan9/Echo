using System;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Mathematics;
using CodeHelpers.Mathematics.Enumerables;
using EchoRenderer.Textures;

namespace EchoRenderer.IO
{
	public class Font
	{
		public Font(string path)
		{
			texture = Texture2D.Load(path);
			glyphSize = texture.size / MapWidth;

			Int2 size = texture.size;

			if (size % MapWidth != Int2.zero) throw new Exception($"Invalid font map texture size {size}.");

			glyphs = new Glyph[MapWidth * MapWidth];

			Parallel.For(0, glyphs.Length, ComputeSingleGlyph);

			void ComputeSingleGlyph(int index)
			{
				Int2 position = new Int2(index % MapWidth, index / MapWidth) * glyphSize;

				Float2 min = Float2.positiveInfinity;
				Float2 max = Float2.negativeInfinity;

				foreach (Int2 local in new EnumerableSpace2D(position, position + glyphSize - Int2.one))
				{
					float red = texture[local].x;
					if (Scalars.AlmostEquals(red, 0f)) continue;

					min = min.Min(local);
					max = max.Max(local);
				}

				max += Float2.one;

				min /= size;
				max /= size;

				Float2 origin = (position + glyphSize / 2f) / size;
				glyphs[index] = new Glyph(min, max, origin);
			}
		}

		public readonly Texture2D texture;
		public readonly Glyph[] glyphs;

		readonly Int2 glyphSize;

		const int MapWidth = 8;

		public void Draw(Texture destination, char character, Style style)
		{
			Glyph glyph = glyphs[GetIndex(character)];


		}

		int GetIndex(char character)
		{
			const int LetterCount = 'Z' - 'A';

			int order = character switch
			{
				>= 'A' and <= 'Z' => character - 'A',
				>= 'a' and <= 'z' => character - 'a' + LetterCount,
				>= '0' and <= '9' => character - '0' + LetterCount * 2,
				_ => throw ExceptionHelper.Invalid(nameof(character), character, InvalidType.unexpected)
			};

			Int2 position = new Int2(order % MapWidth, order / MapWidth);
			position = new Int2(position.x, MapWidth - position.y - 1);

			return position.x + position.y * MapWidth;
		}

		public readonly struct Style
		{
			public Style(Float2 center, float size, Float4 color)
			{
				this.center = center;
				this.size = size;
				this.color = color;
			}

			public readonly Float2 center;
			public readonly float size;
			public readonly Float4 color;
		}

		public readonly struct Glyph
		{
			public Glyph(Float2 minUV, Float2 maxUV, Float2 origin)
			{
				this.minUV = minUV;
				this.maxUV = maxUV;
				this.origin = origin;
			}

			readonly Float2 minUV;
			readonly Float2 maxUV;

			public readonly Float2 origin;

			public bool Contains(Float2 uv) => minUV <= uv && uv <= maxUV;
		}
	}
}