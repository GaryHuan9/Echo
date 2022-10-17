using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Echo.Core.Common;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Memory;

namespace Echo.Core.InOut;

using CharSpan = ReadOnlySpan<char>;

partial class EchoChronicleHierarchyObjects
{
	sealed class SegmentReader : IDisposable
	{
		public SegmentReader(Stream stream) => reader = new StreamReader(stream);

		readonly StreamReader reader;
		readonly char[] buffer = new char[256];

		int currentPosition;
		int currentLength;
		bool currentComment;

		public int CurrentLine { get; private set; } = 1;

		const char NewLine = '\n';
		const char Comment = '#';

		public CharSpan ReadNext()
		{
			if (!SkipWhiteSpace()) return ReadOnlySpan<char>.Empty;

			if (!Grab(new IdentifierPredicate(), out CharSpan identifier)) return identifier;

			if (!identifier.IsEmpty) return identifier;
			Ensure.IsFalse(IsNewLine(buffer[currentPosition]));
			return buffer.AsSpan(currentPosition++, 1);
		}

		public CharSpan PeekNext()
		{
			if (!SkipWhiteSpace()) return ReadOnlySpan<char>.Empty;

			int line = CurrentLine;

			if (!Grab(new IdentifierPredicate(), out CharSpan identifier) || !identifier.IsEmpty)
			{
				currentPosition -= identifier.Length;
				Ensure.IsTrue(currentLength >= 0);
			}
			else identifier = buffer.AsSpan(currentPosition, 1);

			CurrentLine = line;
			return identifier;
		}

		public CharSpan ReadUntil(char match)
		{
			if (SkipWhiteSpace() && Grab(new MatchPredicate(match), out CharSpan result))
			{
				Ensure.AreEqual(match, buffer[currentPosition]);
				if (IsNewLine(match)) ++CurrentLine;

				++currentPosition;
				return result;
			}

			throw new FormatException($"Next match of {match} not found on line {CurrentLine}.");
		}

		public string ReadIdentifier()
		{
			if (SkipWhiteSpace())
			{
				Grab(new IdentifierPredicate(), out CharSpan result);
				if (!result.IsEmpty) return new string(result);
			}

			throw new FormatException($"Next identifier not found on line {CurrentLine}.");
		}

		public FormatException UnexpectedTokenException(CharSpan token) => new($"Encountered unexpected token '{token}' on line {CurrentLine}.");

		[DebuggerHidden]
		[StackTraceHidden]
		public void ThrowIfTokenMismatch(char expected, CharSpan token)
		{
			if (EqualsSingle(token, expected)) return;
			throw new FormatException($"Expecting token '{expected}', however encountered '{token}' on line {CurrentLine}.");
		}

		[DebuggerHidden]
		[StackTraceHidden]
		public void ThrowIfNextMismatch(char expected) => ThrowIfTokenMismatch(expected, ReadNext());

		public void Dispose() => reader?.Dispose();

		bool Grab<T>(T predicate, out CharSpan result) where T : struct, IGrabPredicate
		{
			int start = currentPosition;

			do
			{
				for (; currentPosition < currentLength; currentPosition++)
				{
					char current = buffer[currentPosition];
					if (predicate.Continue(current))
					{
						if (IsNewLine(current)) ++CurrentLine;
						continue;
					}

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

				Ensure.AreEqual(currentLength, currentPosition);
				Span<char> slice = buffer.AsSpan(currentLength);

				if (slice.IsEmpty) throw new FormatException($"Identifier '{buffer.AsSpan(0, 16)}...' exceeds buffer size of {buffer.Length} on line {CurrentLine}.");

				currentLength += Read(slice);
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
					char current = buffer[currentPosition];
					if (!char.IsWhiteSpace(current)) return true;
					if (IsNewLine(current)) ++CurrentLine;
				}

				int read = Read(buffer);
				if (read == 0) return false; //Reached end of file

				currentLength = read;
				currentPosition = 0;
			}
		}

		int Read(Span<char> span)
		{
			CharSpan read = span[..reader.Read(span)];

			int readHead = 0;
			int spanHead = 0;

			while (true)
			{
				int index;

				if (currentComment)
				{
					index = read.IndexOf(NewLine);
					if (index < 0) break;
				}
				else
				{
					index = read.IndexOf(Comment);
					bool endOfBuffer = index < 0;
					index = endOfBuffer ? read.Length : index;

					if (readHead != spanHead) read[..index].CopyTo(span);

					spanHead += index;
					span = span[index..];
					if (endOfBuffer) break;
				}

				readHead += index;
				read = read[index..];

				currentComment = !currentComment;
			}

			return spanHead;
		}

		static bool IsNewLine(char value) => value == NewLine;

		interface IGrabPredicate
		{
			bool Continue(char value);
		}

		readonly struct MatchPredicate : IGrabPredicate
		{
			public MatchPredicate(char match) => this.match = match;

			readonly char match;

			public bool Continue(char value) => value != match;
		}

		readonly struct IdentifierPredicate : IGrabPredicate
		{
			public bool Continue(char value) => ('a' <= value) & (value <= 'z') ||
												('A' <= value) & (value <= 'Z') ||
												('0' <= value) & (value <= '9') || value == '_';
		}
	}

	readonly record struct Identified<T>(string identifier, T node) where T : Node
	{
		public readonly string identifier = identifier;
		public readonly T node = node;
	}

	readonly ref struct ScopeStack
	{
		public ScopeStack() => stack = new List<Dictionary<string, ArgumentNode>>();

		readonly List<Dictionary<string, ArgumentNode>> stack;

		public ReleaseHandle Advance() => new(stack);

		public void Add(Identified<ArgumentNode> item)
		{
			Dictionary<string, ArgumentNode> declarations = stack[^1];

			if (declarations == null)
			{
				declarations = new Dictionary<string, ArgumentNode>(1);
				stack[^1] = declarations;
			}

			declarations.TryAdd(item.identifier, item.node);
		}

		public ArgumentNode Find(string identifier)
		{
			for (int i = stack.Count - 1; i >= 0; i--)
			{
				Dictionary<string, ArgumentNode> declarations = stack[i];
				ArgumentNode node = declarations?.TryGetValue(identifier);
				if (node != null) return node;
			}

			return null;
		}

		public struct ReleaseHandle : IDisposable
		{
			public ReleaseHandle(List<Dictionary<string, ArgumentNode>> stack)
			{
				this.stack = stack;
				stack.Add(null);
			}

			List<Dictionary<string, ArgumentNode>> stack;

			void IDisposable.Dispose()
			{
				if (stack == null) return;
				stack.RemoveAt(stack.Count - 1);
				stack = null;
			}
		}
	}

	sealed class TypeMap
	{
		TypeMap()
		{
			foreach (Type type in typeof(TypeMap).Assembly.GetExportedTypes())
			{
				if (type.IsValueType || type.IsAbstract || type.IsNotPublic || type.IsNestedPrivate) continue;

				CollectionsMarshal.GetValueRefOrAddDefault
				(
					map, type.Name,
					out bool exists
				) = exists ? null : type;
			}
		}

		readonly Dictionary<string, Type> map = new(StringComparer.Ordinal);

		static readonly object locker = new();
		static TypeMap _instance;

		public static TypeMap Instance
		{
			get
			{
				lock (locker) return _instance ??= new TypeMap();
			}
		}

		public Type this[string identifier] => map.TryGetValue(identifier);
	}
}