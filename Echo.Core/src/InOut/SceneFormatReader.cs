using System;
using System.Collections.Generic;
using System.IO;
using CodeHelpers.Diagnostics;

namespace Echo.Core.InOut;

using CharSpan = ReadOnlySpan<char>;

public sealed partial class SceneFormatReader : IDisposable
{
	public SceneFormatReader(string path) : this(File.OpenRead(path)) { }

	public SceneFormatReader(Stream stream, bool leaveOpen = false)
	{
		reader = new SegmentReader(stream, leaveOpen);
		root = ScopeNode.Create(reader, null);
	}

	readonly SegmentReader reader;
	readonly ScopeNode root;

	public void Dispose() => reader?.Dispose();

	static bool IsIdentifier(char value) => (('0' <= value) & (value <= '9')) |
											(('A' <= value) & (value <= 'Z')) |
											(('a' <= value) & (value <= 'z'));

	static bool IsIdentifier(CharSpan value)
	{
		if (value.Length == 0) return false;

		foreach (char current in value)
		{
			if (!IsIdentifier(current)) return false;
		}

		return true;
	}

	sealed class SegmentReader : IDisposable
	{
		public SegmentReader(Stream stream, bool leaveOpen = false) => reader = new StreamReader(stream, leaveOpen);

		readonly StreamReader reader;
		readonly char[] buffer = new char[256];

		int currentPosition;
		int currentLength;

		readonly Dictionary<char, Func<char, bool>> matchPredicateMap = new();

		static readonly Func<char, bool> identifierPredicate = value => !IsIdentifier(value);

		public CharSpan ReadNext()
		{
			if (!SkipWhiteSpace()) return ReadOnlySpan<char>.Empty;

			if (!Grab(identifierPredicate, out CharSpan identifier)) return identifier;
			return identifier.IsEmpty ? buffer.AsSpan(currentPosition++, 1) : identifier;
		}

		public CharSpan PeekNext()
		{
			if (!SkipWhiteSpace()) return ReadOnlySpan<char>.Empty;

			if (!Grab(identifierPredicate, out CharSpan identifier) || !identifier.IsEmpty)
			{
				currentPosition -= identifier.Length;
				Assert.IsTrue(currentLength >= 0);

				return identifier;
			}

			return buffer.AsSpan(currentPosition, 1);
		}

		public CharSpan ReadUntil(char match)
		{
			return SkipWhiteSpace() &&
				   Grab(GetPredicate(), out CharSpan result) &&
				   ++currentPosition is { }
				? result
				: throw new FormatException($"No match of {match} found.");

			Func<char, bool> GetPredicate()
			{
				if (!matchPredicateMap.TryGetValue(match, out var predicate))
				{
					predicate = value => value == match;
					matchPredicateMap.Add(match, predicate);
				}

				return predicate;
			}
		}

		public CharSpan ReadIdentifier() =>
			SkipWhiteSpace() &&
			Grab(identifierPredicate, out CharSpan result) is { } &&
			result is { IsEmpty: false }
				? result
				: throw new FormatException("No identifier found.");

		public void Dispose() => reader?.Dispose();

		bool Grab(Func<char, bool> predicate, out CharSpan result)
		{
			int start = currentPosition;

			do
			{
				for (; currentPosition < currentLength; currentPosition++)
				{
					if (!predicate(buffer[currentPosition])) continue;
					result = buffer.AsSpan(start..currentPosition);
					return true;
				}

				if (start > 0)
				{
					//Move the current segment forward in the buffer
					buffer.AsSpan(start..currentPosition).CopyTo(buffer);
					currentLength -= start;
					currentPosition = currentLength;
					start = 0;
				}

				Assert.AreEqual(currentLength, currentPosition);
				Span<char> slice = buffer.AsSpan(currentLength);

				if (!slice.IsEmpty) currentLength += reader.Read(slice);
				else throw new FormatException("Identifier too long.");
			}
			while (currentPosition < currentLength);

			//Reached end of file
			result = buffer.AsSpan(start..currentPosition);
			return false;
		}

		bool SkipWhiteSpace()
		{
			while (true)
			{
				for (; currentPosition < currentLength; currentPosition++)
				{
					if (!char.IsWhiteSpace(buffer[currentPosition])) return true;
				}

				int read = reader.Read(buffer);
				if (read == 0) return false; //Reached end of file

				currentLength = read;
				currentPosition = 0;
			}
		}
	}
}