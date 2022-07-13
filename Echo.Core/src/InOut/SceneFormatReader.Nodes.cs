using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Transactions;
using CodeHelpers.Collections;

namespace Echo.Core.InOut;

using CharSpan = ReadOnlySpan<char>;

partial class SceneFormatReader
{
	abstract class Node
	{
		protected static FormatException UnexpectedTokenException(CharSpan token) => new($"Encountered unexpected token: {token}.");

		[DebuggerHidden]
		[StackTraceHidden]
		protected static void ThrowIfTokenMismatch(CharSpan token, CharSpan expected)
		{
			if (token.SequenceEqual(expected)) return;
			throw new FormatException($"Expecting token `{expected}`, encountered: {token}.");
		}
	}

	abstract class ArgumentNode : Node
	{
		public static ArgumentNode Create(SegmentReader reader, ScopeStack stack)
		{
			CharSpan next = reader.ReadNext();

			return next.ToString() switch //OPTIMIZE: use https://github.com/dotnet/csharplang/issues/1881 when we switch to dotnet 7 
			{
				"`"    => LiteralNode.Create(reader),
				"new"  => TypedNode.Create(reader, stack),
				"link" => stack.Find(new string(reader.ReadIdentifier())),
				_      => throw UnexpectedTokenException(next)
			};
		}
	}

	sealed class RootNode : Node
	{
		readonly List<Identified<ArgumentNode>> children = new();

		public static RootNode Create(SegmentReader reader)
		{
			ScopeStack stack = new();
			RootNode node = new();

			using var _ = stack.Advance();
			CharSpan next = reader.ReadNext();

			while (!next.IsEmpty)
			{
				ThrowIfTokenMismatch(next, ":");

				string identifier = reader.ReadIdentifier();
				ThrowIfTokenMismatch(reader.ReadNext(), "=");

				Identified<ArgumentNode> identified = new(identifier, ArgumentNode.Create(reader, stack));

				stack.Add(identified);
				node.children.Add(identified);

				next = reader.ReadNext();
			}

			return node;
		}
	}

	sealed class ParametersNode : Node
	{
		readonly List<ArgumentNode> arguments = new();

		public static readonly ParametersNode empty = new();

		public Node this[int index] => arguments[index];

		public static ParametersNode Create(SegmentReader reader, ScopeStack scope)
		{
			CharSpan next = reader.PeekNext();

			if (next.SequenceEqual(")"))
			{
				ThrowIfTokenMismatch(reader.ReadNext(), ")");
				return empty;
			}

			ParametersNode node = new();

			while (true)
			{
				node.arguments.Add(ArgumentNode.Create(reader, scope));

				next = reader.ReadNext();

				if (next.SequenceEqual(")")) break;
				ThrowIfTokenMismatch(next, ",");
			}

			return node;
		}
	}

	sealed class LiteralNode : ArgumentNode
	{
		LiteralNode(CharSpan content) => this.content = new string(content);

		public readonly string content;

		public static LiteralNode Create(SegmentReader reader)
		{
			// CharSpan next = reader.PeekNext();
			//TODO: handle explicitly defined types using `next`
			//note that the returned span can change if we invoke ReadNext again

			return new(reader.ReadUntil('`'));
		}
	}

	sealed class TypedNode : ArgumentNode
	{
		TypedNode(Identified<ParametersNode> constructor) => this.constructor = constructor;

		public readonly Identified<ParametersNode> constructor;

		readonly List<Identified<Node>> children = new();

		new public static TypedNode Create(SegmentReader reader, ScopeStack stack)
		{
			string type = reader.ReadIdentifier();
			CharSpan next = reader.PeekNext();
			ParametersNode parameters;

			if (next.SequenceEqual("("))
			{
				ThrowIfTokenMismatch(reader.ReadNext(), "(");
				parameters = ParametersNode.Create(reader, stack);
				next = reader.PeekNext();
			}
			else parameters = ParametersNode.empty;

			TypedNode node = new(new Identified<ParametersNode>(type, parameters));

			if (next.SequenceEqual("{"))
			{
				using var _ = stack.Advance();

				ThrowIfTokenMismatch(reader.ReadNext(), "{");
				next = reader.ReadNext();

				while (!next.SequenceEqual("}"))
				{
					if (next.SequenceEqual("."))
					{
						string identifier = reader.ReadIdentifier();
						Node child;

						next = reader.ReadNext();

						if (next.SequenceEqual("=")) child = ArgumentNode.Create(reader, stack);
						else if (next.SequenceEqual("(")) child = ParametersNode.Create(reader, stack);
						else throw UnexpectedTokenException(next);

						node.children.Add(new Identified<Node>(identifier, child));
					}
					else if (next.SequenceEqual(":"))
					{
						string identifier = reader.ReadIdentifier();
						ThrowIfTokenMismatch(reader.ReadNext(), "=");

						stack.Add(new Identified<ArgumentNode>(identifier, ArgumentNode.Create(reader, stack)));
					}
					else throw UnexpectedTokenException(next);

					next = reader.ReadNext();
				}
			}

			return node;
		}
	}

	readonly struct Identified<T> where T : Node
	{
		public Identified(string identifier, T node)
		{
			this.identifier = identifier;
			this.node = node;
		}

		public readonly string identifier;
		public readonly T node;
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
}