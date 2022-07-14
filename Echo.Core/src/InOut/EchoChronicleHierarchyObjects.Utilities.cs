using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;

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

		readonly Dictionary<char, Func<char, bool>> matchPredicateMap = new();

		static readonly Func<char, bool> identifierPredicate = value => !((('0' <= value) & (value <= '9')) |
																		  (('A' <= value) & (value <= 'Z')) |
																		  (('a' <= value) & (value <= 'z')));

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

		public string ReadIdentifier() =>
			SkipWhiteSpace() &&
			Grab(identifierPredicate, out CharSpan result) is { } &&
			result is { IsEmpty: false }
				? new string(result)
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
		public TypeMap()
		{
			foreach (Type type in typeof(TypeMap).Assembly.GetExportedTypes())
			{
				if (type.IsValueType || type.IsAbstract) continue;

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