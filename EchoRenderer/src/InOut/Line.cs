using System;

namespace EchoRenderer.InOut;

public readonly struct Line
{
	public Line(string source, int height = -1) : this(source, height, Range.All) { }

	public Line(string source, int height, Range range)
	{
		this.height = height;
		this.source = source;

		(offset, length) = range.GetOffsetAndLength(source.Length);
	}

	public readonly int height;
	public readonly int length;

	readonly string source;
	readonly int offset;

	public bool IsEmpty => length == 0;

	public char this[int value] => source[offset + value];

	public Line this[Range value]
	{
		get
		{
			(int Offset, int Length) set = value.GetOffsetAndLength(length);

			int start = offset + set.Offset;
			return new Line(source, height, start..(start + set.Length));
		}
	}

	public Line Trim()
	{
		int start = 0;
		int end = length;

		while (char.IsWhiteSpace(this[start])) start++;
		while (char.IsWhiteSpace(this[end - 1])) end--;

		return this[start..end];
	}

	public Line Trim(char character)
	{
		int start = 0;
		int end = length;

		while (this[start] == character) start++;
		while (this[end - 1] == character) end--;

		return this[start..end];
	}

	public static implicit operator ReadOnlySpan<char>(Line line) => ((ReadOnlySpan<char>)line.source).Slice(line.offset, line.length);
	public static implicit operator string(Line line) => line.source.Substring(line.offset, line.length);

	public override string ToString() => height < 0 ? this : $"{this} ({height})";
}