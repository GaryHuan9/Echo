using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;

namespace Echo.Terminal.Core;

using CharSpan = ReadOnlySpan<char>;

public partial struct Domain
{
	public readonly struct Drawer
	{
		public Drawer(in Domain domain, bool invertY = false)
		{
			if (domain.size.MinComponent < 1) throw new ArgumentException("Invalid size", nameof(domain));

			size = domain.size;
			array = domain.array;
			stride = CalculateStride(domain, invertY);
			offset = CalculateOffset(domain, invertY);
		}

		public readonly Int2 size;
		readonly char[] array;
		readonly int stride;
		readonly int offset;

		public ref char this[Int2 position] => ref this[position.X, position.Y];

		public ref char this[int x, int y]
		{
			get
			{
				Assert.IsTrue(new Int2(x, y) >= Int2.Zero);
				Assert.IsTrue(new Int2(x, y) < size);
				return ref array[y * stride + x + offset];
			}
		}

		public Span<char> this[int y]
		{
			get
			{
				Assert.IsTrue(0 <= y && y < size.Y);
				return array.AsSpan(y * stride + offset, size.X);
			}
		}

		public void FillAll(char value = ' ') => FillAll(0, value);

		public int FillAll(int y, char value = ' ')
		{
			if (CheckBounds(y)) return y;
			for (; y < size.Y; y++) FillLine(y, value);
			return y;
		}

		public Int2 FillAll(Int2 position, char value = ' ')
		{
			position = FillLine(position, value);
			return new Int2(0, FillAll(position.Y));
		}

		public int FillLine(int y, char value = ' ')
		{
			if (CheckBounds(y)) return y;
			this[y].Fill(value);
			return y + 1;
		}

		public Int2 FillLine(Int2 position, char value = ' ')
		{
			if (CheckBounds(position)) return position;
			this[position.Y][position.X..].Fill(value);
			return new Int2(0, position.Y + 1);
		}

		public int WriteLine(int y, CharSpan texts, in TextOptions options = default) => WriteLine(new Int2(0, y), texts, options).Y;

		public Int2 WriteLine(Int2 position, CharSpan texts, in TextOptions options = default)
		{
			position = Write(position, texts, options);
			if (position.X == 0) return position;
			return FillLine(position);
		}

		public int Write(int y, CharSpan texts, in TextOptions options = default) => Write(new Int2(0, y), texts, options).Y;

		public Int2 Write(Int2 position, CharSpan texts, in TextOptions options = default)
		{
			if (CheckBounds(position)) return position;

			switch (options.WrapOptions)
			{
				case WrapOptions.Justified: break;
				case WrapOptions.WordBreak: break;
				case WrapOptions.LineBreak:
				{
					int space = size.X - position.X;

					while (texts.Length > space && position.Y + 1 < size.Y)
					{
						var slice = texts[..space];
						texts = texts[space..];

						position = WriteShort(position, slice);
						space = size.X;
					}

					goto case WrapOptions.NoWrap;
				}
				case WrapOptions.NoWrap:
				{
					if (texts.IsEmpty) return position;

					int space = size.X - position.X;
					bool tooLong = texts.Length > space;
					if (tooLong) texts = texts[..space];

					if (tooLong && !options.Truncate)
					{
						position = WriteShort(position, texts);
						this[size.X - 1, position.Y - 1] = '…';

						return position;
					}

					return WriteShort(position, texts);
				}
				default: throw new ArgumentOutOfRangeException(nameof(options));
			}

			throw new NotImplementedException();
		}

		Int2 WriteShort(Int2 position, CharSpan value)
		{
			Span<char> span = this[position.Y][position.X..];
			Assert.IsTrue(value.Length <= span.Length);

			value.CopyTo(span);

			position += new Int2(value.Length, 0);
			if (position.X < size.X) return position;
			return new Int2(0, position.Y + 1);
		}

		bool CheckBounds(int y)
		{
			if ((0 <= y) & (y <= size.Y)) return y == size.Y;
			throw new ArgumentOutOfRangeException(nameof(y));
		}

		bool CheckBounds(Int2 position)
		{
			if ((Int2.Zero <= position) & (position < size)) return false;
			if ((position.X == 0) & (position.Y == size.Y)) return true;
			throw new ArgumentOutOfRangeException(nameof(position));
		}

		static int CalculateStride(in Domain domain, bool invertY) => invertY ? domain.realSize.X : -domain.realSize.X;

		static int CalculateOffset(in Domain domain, bool invertY)
		{
			int offsetY = invertY ? domain.size.Y : 1;
			return domain.realSize.X * (domain.realSize.Y - domain.origin.Y - offsetY) + domain.origin.X;
		}

		static int LastIndexOfWhiteSpace(ReadOnlySpan<char> span)
		{
			for (int i = span.Length - 1; i >= 0; i--)
			{
				if (char.IsWhiteSpace(span[i])) return i;
			}

			return -1;
		}
	}
}