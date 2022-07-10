using System;
using System.IO;

namespace Echo.Core.InOut;

public sealed class SceneFormatReader : IDisposable
{
	public SceneFormatReader(string path) : this(File.OpenRead(path)) { }

	public SceneFormatReader(Stream stream, bool leaveOpen = false) => reader = new SegmentReader(stream, leaveOpen);

	readonly SegmentReader reader;

	public void Read()
	{
		ReadOnlySpan<char> token = reader.ReadSegment();

		while (!token.IsEmpty)
		{
			Console.WriteLine(token.ToString());
			token = reader.ReadSegment();
		}
	}

	public void Dispose() => reader?.Dispose();

	sealed class SegmentReader : IDisposable
	{
		public SegmentReader(Stream stream, bool leaveOpen = false) => reader = new StreamReader(stream, leaveOpen);

		readonly StreamReader reader;
		readonly char[] buffer = new char[32];

		int currentPosition;
		int currentLength;

		public ReadOnlySpan<char> ReadSegment() => ReadSegmentImpl(default);

		public ReadOnlySpan<char> ReadSegment(char match) => ReadSegmentImpl(match);

		ReadOnlySpan<char> ReadSegmentImpl(char match)
		{
			if (!SkipWhiteSpace()) return ReadOnlySpan<char>.Empty;

			int start = currentPosition;

			do
			{
				for (; currentPosition < currentLength; currentPosition++)
				{
					char current = buffer[currentPosition];

					if (match == default
						? char.IsWhiteSpace(current)
						: match == current) goto exit;
				}

				if (start > 0)
				{
					//Move the current segment forward in the buffer
					buffer.AsSpan(start..currentPosition).CopyTo(buffer);
					currentLength -= start;
					currentPosition = currentLength;
					start = 0;
				}

				var slice = buffer.AsSpan(currentLength);

				if (!slice.IsEmpty) currentLength += reader.Read(slice);
				else throw new Exception("Parsing segment is too long.");
			}
			while (currentLength > currentPosition);

		exit:
			return buffer.AsSpan(start..currentPosition++);
		}

		bool SkipWhiteSpace()
		{
			do
			{
				for (; currentPosition < currentLength; currentPosition++)
				{
					if (!char.IsWhiteSpace(buffer[currentPosition])) return true;
				}

				currentLength = reader.Read(buffer);
				currentPosition = 0;
			}
			while (currentLength > 0);

			//Reached end of line
			return false;
		}

		public void Dispose() => reader?.Dispose();
	}
}