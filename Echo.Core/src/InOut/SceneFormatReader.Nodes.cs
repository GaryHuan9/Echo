using System;
using System.Collections.Generic;
using System.Diagnostics;

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
		public static ArgumentNode Create(SegmentReader reader, ScopeNode scope)
		{
			CharSpan next = reader.ReadNext();

			//OPTIMIZE: use https://github.com/dotnet/csharplang/issues/1881 when we switch to dotnet 7 
			return next.ToString() switch
			{
				"["    => LiteralNode.Create(reader),
				"new"  => TypedNode.Create(reader, scope),
				"link" => LinkNode.Create(reader, scope),
				_      => throw UnexpectedTokenException(next)
			};
		}
	}

	sealed class LiteralNode : ArgumentNode
	{
		LiteralNode(CharSpan content) => this.content = new string(content);

		public readonly string content;

		public static LiteralNode Create(SegmentReader reader) => new(reader.ReadUntil(']'));
	}

	sealed class TypedNode : ArgumentNode
	{
		TypedNode(ScopeNode scope, InvocationNode constructor)
		{
			this.scope = scope;
			this.constructor = constructor;
		}

		public readonly ScopeNode scope;
		public readonly InvocationNode constructor;

		new public static TypedNode Create(SegmentReader reader, ScopeNode scope)
		{
			CharSpan type = reader.ReadIdentifier();
			CharSpan next = reader.ReadNext();
			InvocationNode constructor;

			if (next.SequenceEqual("("))
			{
				constructor = InvocationNode.Create(reader, scope, type);
				next = reader.ReadNext();
			}
			else constructor = InvocationNode.Create(type);

			ThrowIfTokenMismatch(next, "{");

			return new TypedNode(ScopeNode.Create(reader, scope), constructor);
		}
	}

	sealed class LinkNode : ArgumentNode
	{
		LinkNode(ScopeNode parent, CharSpan identifier, InvocationNode invocation)
		{
			this.parent = parent;
			this.identifier = new string(identifier);
			this.invocation = invocation;
		}

		public readonly ScopeNode parent;
		public readonly string identifier;
		public readonly InvocationNode invocation;

		new public static LinkNode Create(SegmentReader reader, ScopeNode parent)
		{
			CharSpan identifier = reader.ReadIdentifier();

			if (reader.PeekNext().SequenceEqual("."))
			{
				ThrowIfTokenMismatch(reader.ReadNext(), ".");
				CharSpan secondary = reader.ReadIdentifier();

				if (reader.PeekNext().SequenceEqual("("))
				{
					ThrowIfTokenMismatch(reader.ReadNext(), "(");
					var invocation = InvocationNode.Create(reader, parent, secondary);
					return new LinkNode(parent, identifier, invocation);
				}

				return new LinkNode(parent, identifier, InvocationNode.Create(secondary));
			}

			return new LinkNode(parent, identifier, null);
		}
	}

	sealed class ScopeNode : Node
	{
		ScopeNode(ScopeNode parent) => this.parent = parent;

		readonly ScopeNode parent;

		readonly Dictionary<string, ArgumentNode> declarations = new();
		readonly Dictionary<string, ArgumentNode> assignments = new();
		readonly List<InvocationNode> invocations = new();

		public static ScopeNode Create(SegmentReader reader, ScopeNode parent)
		{
			var node = new ScopeNode(parent);
			CharSpan next = reader.ReadNext();

			while (!next.SequenceEqual("}"))
			{
				if (next.SequenceEqual(":"))
				{
					CharSpan identifier = reader.ReadIdentifier();

					ThrowIfTokenMismatch(reader.ReadNext(), "=");
					AddAssignment(node.declarations, identifier);
				}
				else if (next.SequenceEqual(".") && parent != null)
				{
					CharSpan identifier = reader.ReadIdentifier();

					next = reader.ReadNext();

					if (next.SequenceEqual("=")) AddAssignment(node.assignments, identifier);
					else if (next.SequenceEqual("(")) AddInvocation(identifier);
					else throw UnexpectedTokenException(next);
				}
				else if (next.IsEmpty && parent == null) break;
				else throw UnexpectedTokenException(next);

				next = reader.ReadNext();

				void AddAssignment(Dictionary<string, ArgumentNode> map, CharSpan identifier)
				{
					var converted = new string(identifier);

					if (!map.ContainsKey(converted)) map.Add(converted, ArgumentNode.Create(reader, node));
					else throw new FormatException($"Duplicated identifier '{converted}' for assignment.");
				}

				void AddInvocation(CharSpan identifier) => node.invocations.Add(InvocationNode.Create(reader, node, identifier));
			}

			return node;
		}
	}

	sealed class InvocationNode : Node
	{
		InvocationNode(CharSpan identifier) => this.identifier = new string(identifier);

		public readonly string identifier;

		readonly List<ArgumentNode> arguments = new();

		public Node this[int index] => arguments[index];

		public static InvocationNode Create(SegmentReader reader, ScopeNode parent, CharSpan identifier)
		{
			var next = reader.PeekNext();
			var node = Create(identifier);

			if (next.SequenceEqual(")"))
			{
				reader.ReadNext();
				return node;
			}

			while (true)
			{
				node.arguments.Add(ArgumentNode.Create(reader, parent));

				next = reader.ReadNext();

				if (next.SequenceEqual(")")) return node;
				ThrowIfTokenMismatch(next, ",");
			}
		}

		public static InvocationNode Create(CharSpan identifier) => new(identifier);
	}
}