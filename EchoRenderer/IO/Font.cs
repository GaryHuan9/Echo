using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Mathematics;
using CodeHelpers.Mathematics.Enumerables;
using EchoRenderer.Mathematics;
using EchoRenderer.Textures;

namespace EchoRenderer.IO
{
	public class Font
	{
		public Font(string path)
		{
			texture = Texture2D.Load(path);

			Int2 size = texture.size;
			Int2 glyphSize = size / MapSize;

			if (size % MapSize != Int2.zero) throw new Exception($"Invalid font map texture size {size}.");

			glyphs = new Glyph[MapSize * MapSize];
			GlyphAspect = 0.6f * glyphSize.x / glyphSize.y;

			Parallel.For(0, glyphs.Length, ComputeSingleGlyph);

			void ComputeSingleGlyph(int index)
			{
				Int2 position = new Int2(index % MapSize, index / MapSize) * glyphSize;

				Float2 min = Float2.positiveInfinity;
				Float2 max = Float2.negativeInfinity;

				foreach (Int2 local in new EnumerableSpace2D(position, position + glyphSize - Int2.one))
				{
					float strength = texture[local].x;
					texture[local] = (Float4)strength;

					if (Scalars.AlmostEquals(strength, 0f)) continue;

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

		public float GlyphAspect { get; set; }
		public int SampleSize { get; set; } = 5;

		readonly Texture2D texture;
		readonly Glyph[] glyphs;

		const int MapSize = 8;

		/// <summary>
		/// Draws <paramref name="character"/> to <paramref name="destination"/> with this <see cref="Font"/> and <paramref name="style"/>.
		/// </summary>
		public void Draw(Texture destination, char character, Style style)
		{
			Glyph glyph = glyphs[GetIndex(character)];
			float multiplier = MapSize * style.height;

			Float2 min = (glyph.minUV - glyph.origin) * multiplier + style.center;
			Float2 max = (glyph.maxUV - glyph.origin) * multiplier + style.center;

			Float4 colorSource = style.color.Replace(3, 1f);

			Vector128<float> color = Utilities.ToVector(colorSource);
			Vector128<float> alpha = Vector128.Create(style.color.w);

			Parallel.ForEach(new EnumerableSpace2D(min.Floored, max.Ceiled), DrawPixel);

			void DrawPixel(Int2 position)
			{
				Vector128<float> total = Vector128<float>.Zero;

				//Take multiple samples and calculate the average
				foreach (Int2 offset in new EnumerableSpace2D(Int2.one, (Int2)SampleSize))
				{
					Float2 point = (position + offset / (SampleSize + 1f) - style.center) / multiplier + glyph.origin;
					if (glyph.minUV <= point && point <= glyph.maxUV) total = Sse.Add(total, texture.GetPixel(point));
				}

				total = Sse.Divide(total, Vector128.Create((float)SampleSize * SampleSize));

				ref Vector128<float> target = ref destination.GetPixel(position);
				target = Utilities.Lerp(target, color, Sse.Multiply(alpha, total));
			}
		}

		/// <summary>
		/// Draws <paramref name="text"/> to <paramref name="destination"/> with this <see cref="Font"/> and <paramref name="style"/>.
		/// </summary>
		public void Draw(Texture destination, string text, in Style style)
		{
			for (int i = 0; i < text.Length; i++)
			{
				float x = (i - text.Length / 2f + 0.5f) * GlyphAspect * style.height;

				Float2 position = style.center + new Float2(x, 0f);
				Style single = style.ReplaceCenter(position);

				char character = text[i];

				if (char.IsWhiteSpace(character)) continue;
				Draw(destination, character, single);
			}
		}

		/// <summary>
		/// Calculates the drawing region size of string with <paramref name="length"/> using <paramref name="style"/>.
		/// </summary>
		public Float2 GetDrawArea(int length, in Style style)
		{
			float width = (length / 2f + 0.5f) * GlyphAspect;
			return new Float2(width, 1f) * style.height;
		}

		static int GetIndex(char character)
		{
			const int LetterCount = 'Z' - 'A' + 1;

			int order = character switch
			{
				>= 'A' and <= 'Z' => character - 'A',
				>= 'a' and <= 'z' => character - 'a' + LetterCount,
				>= '0' and <= '9' => character - '0' + LetterCount * 2,
				_ => throw ExceptionHelper.Invalid(nameof(character), character, InvalidType.unexpected)
			};

			Int2 position = new Int2(order % MapSize, order / MapSize);
			position = new Int2(position.x, MapSize - position.y - 1);

			return position.x + position.y * MapSize;
		}

		public readonly struct Style
		{
			public Style(Float2 center, float height, Float4 color)
			{
				this.center = center;
				this.height = height;
				this.color = color;
			}

			public readonly Float2 center;
			public readonly float height;
			public readonly Float4 color;

			public Style ReplaceCenter(Float2 value) => new Style(value, height, color);
		}

		readonly struct Glyph
		{
			public Glyph(Float2 minUV, Float2 maxUV, Float2 origin)
			{
				this.minUV = minUV;
				this.maxUV = maxUV;
				this.origin = origin;
			}

			public readonly Float2 minUV;
			public readonly Float2 maxUV;
			public readonly Float2 origin;
		}
	}
}