using System;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;

namespace Echo.Terminal.Core.Display;

using CharSpan = ReadOnlySpan<char>;

public readonly struct Canvas
{
	public Canvas(Int2 size, char[] array, int stride, int offset)
	{
		Ensure.IsTrue(size > Int2.Zero);
		Ensure.IsNotNull(array);

		this.size = size;
		this.array = array;
		this.stride = stride;
		this.offset = offset;
	}

	/// <summary>
	/// The width the height of the drawable space.
	/// </summary>
	public readonly Int2 size;

	readonly char[] array;
	readonly int stride;
	readonly int offset;

	/// <summary>
	/// Accesses a location on this <see cref="Canvas"/>.
	/// </summary>
	/// <param name="position">The location to access.</param>
	public ref char this[Int2 position] => ref this[position.X, position.Y];

	/// <summary>
	/// Accesses a location on this <see cref="Canvas"/> from a <see cref="Brush"/>.
	/// </summary>
	/// <param name="brush">The access <see cref="Brush"/>.</param>
	public ref char this[in Brush brush] => ref this[brush.X, brush.Y];

	/// <summary>
	/// Accesses a location on this <see cref="Canvas"/>.
	/// </summary>
	/// <param name="x">The X location to access.</param>
	/// <param name="y">The Y location to access.</param>
	public ref char this[int x, int y]
	{
		get
		{
			Ensure.IsTrue(new Int2(x, y) >= Int2.Zero);
			Ensure.IsTrue(new Int2(x, y) < size);
			return ref array[y * stride + x + offset];
		}
	}

	/// <summary>
	/// Accesses a horizontal row through a <see cref="Span{T}"/>.
	/// </summary>
	/// <param name="y">The Y location of the row to access.</param>
	public Span<char> this[int y]
	{
		get
		{
			Ensure.IsTrue(0 <= y && y < size.Y);
			return array.AsSpan(y * stride + offset, size.X);
		}
	}

	/// <summary>
	/// Fills the entirety of this <see cref="Canvas"/> with one <see cref="char"/>.
	/// </summary>
	/// <param name="value">The <see cref="char"/> to fill.</param>
	public void FillAll(char value = ' ')
	{
		for (int y = 0; y < size.Y; y++) this[y].Fill(value);
	}

	/// <summary>
	/// Fills all available space on this <see cref="Canvas"/> with one <see cref="char"/>.
	/// </summary>
	/// <param name="brush">The <see cref="Brush"/> to use.</param>
	/// <param name="value">The <see cref="char"/> to fill.</param>
	public void FillAll(ref Brush brush, char value = ' ')
	{
		if (brush.CheckBounds(size)) return;
		this[brush.Y][brush.X..].Fill(value);

		for (int y = brush.Y + 1; y < size.Y; y++) this[y].Fill(value);
		brush.Position = new Int2(0, size.Y);
	}

	/// <summary>
	/// Fills an entire horizontal line on this <see cref="Canvas"/> with one <see cref="char"/> and ends in a new line.
	/// </summary>
	/// <param name="brush">The <see cref="Brush"/> to use.</param>
	/// <param name="value">The <see cref="char"/> to fill.</param>
	public void FillLine(ref Brush brush, char value = ' ')
	{
		if (brush.CheckBounds(size)) return;
		this[brush.Y][brush.X..].Fill(value);
		brush.NextLine();
	}

	/// <inheritdoc cref="WriteLine(ref Brush, ReadOnlySpan{char})"/>
	public void WriteLine<T>(ref Brush brush, T item, string format = default, IFormatProvider provider = default) where T : ISpanFormattable
	{
		CharSpan texts = AsText(stackalloc char[size.X], item, format, provider, out var handle);
		using (handle) WriteLine(ref brush, texts);
	}

	// /// <inheritdoc cref="WriteLine(ref Brush, ReadOnlySpan{char})"/>
	// public void WriteLine(ref Brush brush, PainterInterpolatedStringHandler item) => throw new NotImplementedException();

	/// <summary>
	/// Writes a string of texts to this <see cref="Canvas"/> and ends in a new line.
	/// </summary>
	/// <param name="brush">The <see cref="Brush"/> to use.</param>
	/// <param name="item">The object to write as texts.</param>
	public void WriteLine(ref Brush brush, CharSpan item)
	{
		WriteImpl(ref brush, item);
		if (brush.X > 0) FillLine(ref brush);
	}

	/// <inheritdoc cref="Write(ref Brush, ReadOnlySpan{char})"/>
	public void Write<T>(ref Brush brush, T item, string format = default, IFormatProvider provider = default) where T : ISpanFormattable
	{
		CharSpan texts = AsText(stackalloc char[size.X], item, format, provider, out var handle);
		using (handle) Write(ref brush, texts);
	}

	// /// <inheritdoc cref="Write(ref Brush, ReadOnlySpan{char})"/>
	// public void Write(ref Brush brush, PainterInterpolatedStringHandler item) => throw new NotImplementedException();

	/// <summary>
	/// Writes a string of texts to this <see cref="Canvas"/>.
	/// </summary>
	/// <param name="brush">The <see cref="Brush"/> to use.</param>
	/// <param name="item">The object to write as texts.</param>
	public void Write(ref Brush brush, CharSpan item) => WriteImpl(ref brush, item);

	/// <summary>
	/// Shifts a <see cref="Brush"/> on this <see cref="Canvas"/> horizontally.
	/// </summary>
	/// <param name="brush">The <see cref="Brush"/> to shift.</param>
	/// <param name="value">The amount to shift; can be negative.</param>
	/// <remarks>This operation will wrap around the left and right edges and be clamp between the top and bottom edges.</remarks>
	public void Shift(ref Brush brush, int value)
	{
		brush.CheckBounds(size);
		brush.X += value;

		while (brush.X >= size.X)
		{
			if (brush.Y + 1 >= size.Y)
			{
				brush.Position = new Int2(0, size.Y);
				return;
			}

			brush.Position = new Int2(brush.X - size.X, brush.Y + 1);
		}

		while (brush.X < 0)
		{
			if (brush.Y <= 0)
			{
				brush.Position = Int2.Zero;
				return;
			}

			brush.Position = new Int2(brush.X + size.X, brush.Y - 1);
		}
	}

	void WriteImpl(ref Brush brush, CharSpan texts)
	{
		if (brush.CheckBounds(size)) return;

		WrapOptions wrap = brush.options.Wrap;
		Span<char> span = this[brush.Y][brush.X..];
		if (wrap == WrapOptions.NoWrap) goto finalLine;

		//Write multi-line
		while (texts.Length > span.Length && brush.Y + 1 < size.Y)
		{
			if (wrap == WrapOptions.LineBreak)
			{
				//Simply chop the text with line break
				texts[..span.Length].CopyTo(span);
				texts = texts[span.Length..];
			}
			else
			{
				//Find appropriate word break location
				int end = span.Length;

				if (!char.IsWhiteSpace(texts[end]))
				{
					int index = LastIndexOfWhiteSpace(texts[..end]);

					if (index >= 0) end = index;
					else if (brush.X > 0)
					{
						//Skip this line if there is not space for even one word
						//and the width of this line is not at the maximum width

						FillLine(ref brush);
						span = this[brush.Y];
						continue;
					}
				}

				//Execute different copy operation based on options
				switch (wrap)
				{
					case WrapOptions.Justified:
					{
						JustifiedCopy(texts[..end], span);
						break;
					}
					case WrapOptions.WordBreak:
					{
						OverwriteCopy(texts[..end], span);
						break;
					}
					case WrapOptions.LineBreak:
					case WrapOptions.NoWrap:
					default: throw new ArgumentOutOfRangeException(nameof(brush));
				}

				texts = texts[end..].TrimStart();
			}

			//Move to the next line
			brush.NextLine();
			span = this[brush.Y];
		}

	finalLine:
		//Write the last line (a single line)
		if (texts.Length > span.Length)
		{
			texts[..span.Length].CopyTo(span);
			if (brush.options.Ellipsis) span[^1] = '…';

			brush.NextLine();
		}
		else if (texts.Length == span.Length)
		{
			texts.CopyTo(span);
			brush.NextLine();
		}
		else
		{
			if (texts.IsEmpty) return;

			brush.X += texts.Length;
			OverwriteCopy(texts, span);
		}
	}

	static int LastIndexOfWhiteSpace(ReadOnlySpan<char> span)
	{
		for (int i = span.Length - 1; i >= 0; i--)
		{
			if (char.IsWhiteSpace(span[i])) return i;
		}

		return -1;
	}

	static void OverwriteCopy(CharSpan source, Span<char> target)
	{
		source.CopyTo(target);
		if (target.Length == source.Length) return;
		target[source.Length..].Fill(' ');
	}

	static void JustifiedCopy(CharSpan source, Span<char> target)
	{
		source = source.TrimEnd();
		Ensure.IsTrue(source.Length <= target.Length);
		CopyStart(ref source, ref target, true);

		Count(source, out int wordCount, out int charCount);

		if (wordCount <= 1)
		{
			OverwriteCopy(source, target);
			return;
		}

		int gap = wordCount - 1;               //The number of gaps between words in source
		int space = target.Length - charCount; //The total count of spaces to be added
		int average = space / gap;             //The average number of spaces per gap (rounded down)
		int remain = space - average * gap;    //The remain number of spaces because of the round down

		for (int i = 0; i < gap; i++)
		{
			CopyStart(ref source, ref target, false);
			int count = average + (i < remain ? 1 : 0);

			target[..count].Fill(' ');
			target = target[count..];
			source = source.TrimStart();
		}

		Ensure.AreEqual(source.Length, target.Length);
		source.CopyTo(target);

		static void CopyStart(ref CharSpan source, ref Span<char> target, bool whiteSpace)
		{
			int index = 0;

			for (; index < source.Length; index++)
			{
				char value = source[index];
				if (char.IsWhiteSpace(value) != whiteSpace) break;
				target[index] = value;
			}

			if (index == 0) return;
			source = source[index..];
			target = target[index..];
		}

		static void Count(CharSpan source, out int wordCount, out int charCount)
		{
			wordCount = 0;
			charCount = 0;

			bool wasSpace = true;

			foreach (char value in source)
			{
				bool isSpace = char.IsWhiteSpace(value);
				if (!isSpace & wasSpace) ++wordCount;
				if (!isSpace) ++charCount;

				wasSpace = isSpace;
			}
		}
	}

	static CharSpan AsText<T>(Span<char> stack, T item, string format, IFormatProvider provider, out Pool<char>.ReleaseHandle handle) where T : ISpanFormattable
	{
		if (item.TryFormat(stack, out int length, format, provider))
		{
			handle = default;
			return stack[..length];
		}

		handle = Pool<char>.Fetch(stack.Length * 4, out View<char> view);
		if (item.TryFormat(view, out length, format, provider)) return view[..length];

		return item.ToString(format, provider);
	}
}