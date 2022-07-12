using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

			return next.ToString() switch //OPTIMIZE: use https://github.com/dotnet/csharplang/issues/1881 when we switch to dotnet 7 
			{
				"`"    => LiteralNode.Create(reader),
				"new"  => TypedNode.Create(reader, scope),
				"link" => ReferenceNode.Create(reader, scope),
				_      => throw UnexpectedTokenException(next)
			};
		}
	}

	sealed class LiteralNode : ArgumentNode
	{
		LiteralNode(CharSpan content) => this.content = new string(content);

		public readonly string content;

		public static LiteralNode Create(SegmentReader reader)
		{
			CharSpan next = reader.PeekNext();
			//TODO: handle explicitly defined types using `next`

			return new(reader.ReadUntil('`'));
		}
	}

	sealed class TypedNode : ArgumentNode
	{
		TypedNode(ScopeNode scope, CharSpan type, InvocationNode constructor)
		{
			this.scope = scope;
			this.type = new string(type);
			this.constructor = constructor;
		}

		public readonly ScopeNode scope;
		public readonly string type;
		public readonly InvocationNode constructor;

		new public static TypedNode Create(SegmentReader reader, ScopeNode scope)
		{
			CharSpan type = reader.ReadIdentifier();
			CharSpan next = reader.PeekNext();
			InvocationNode constructor;
			ScopeNode innerScope;

			if (next.SequenceEqual("("))
			{
				ThrowIfTokenMismatch(reader.ReadNext(), "(");
				constructor = InvocationNode.Create(reader, scope);
				next = reader.PeekNext();
			}
			else constructor = InvocationNode.empty;

			if (next.SequenceEqual("{"))
			{
				ThrowIfTokenMismatch(reader.ReadNext(), "{");
				innerScope = ScopeNode.Create(reader, scope);
			}
			else innerScope = scope;

			return new TypedNode(innerScope, type, constructor);
		}
	}

	sealed class ReferenceNode : ArgumentNode
	{
		ReferenceNode(ScopeNode scope, InvocationNode invocation, List<Reference> references)
		{
			this.scope = scope;
			this.invocation = invocation;
			this.references = references;
		}

		public readonly ScopeNode scope;
		public readonly InvocationNode invocation;
		readonly List<Reference> references;

		new public static ReferenceNode Create(SegmentReader reader, ScopeNode scope)
		{
			CharSpan next = reader.ReadNext();
			List<Reference> references = new();

			while(true)
			{
				bool declared;

				if (next.SequenceEqual(":")) declared = true;
				else if (next.SequenceEqual(".") && !scope.IsRoot) declared = false;
				else throw UnexpectedTokenException(next);

				references.Add(new Reference(declared, reader.ReadIdentifier()));

				next = reader.ReadNext();

				if (next.SequenceEqual(";")) break;
				else if (next.SequenceEqual("(")) ref
			}

			// CharSpan identifier = reader.ReadIdentifier();
			//
			// if (reader.PeekNext().SequenceEqual("."))
			// {
			// 	ThrowIfTokenMismatch(reader.ReadNext(), ".");
			// 	CharSpan secondary = reader.ReadIdentifier();
			//
			// 	if (reader.PeekNext().SequenceEqual("("))
			// 	{
			// 		ThrowIfTokenMismatch(reader.ReadNext(), "(");
			// 		var invocation = ParametersNode.Create(reader, scope, secondary);
			// 		return new LinkNode(scope, identifier, invocation);
			// 	}
			//
			// 	return new LinkNode(scope, identifier, ParametersNode.Create(secondary));
			// }
			//
			// return new LinkNode(scope, identifier, null);
		}

		readonly struct Reference
		{
			public Reference(bool declared, CharSpan identifier)
			{
				this.declared = declared;
				this.identifier = new string(identifier);
			}

			public readonly bool declared;
			public readonly string identifier;
		}
	}

	sealed class ScopeNode : Node
	{
		ScopeNode(ScopeNode parent) => this.parent = parent;

		readonly ScopeNode parent;

		readonly Dictionary<string, ArgumentNode> declarations = new();
		readonly Dictionary<string, ArgumentNode> assignments = new();
		readonly List<ReferenceNode> invocations = new();

		public bool IsRoot => parent == null;

		public ArgumentNode Find(string identifier)
		{
			ScopeNode current = this;

			do
			{
				if (declarations.TryGetValue(identifier, out var node)) return node;
				current = current.parent;
			}
			while (current != null);

			return null;
		}

		public static ScopeNode Create(SegmentReader reader, ScopeNode parent)
		{
			ScopeNode node = new(parent);
			CharSpan next = reader.ReadNext();
			string endToken = node.IsRoot ? "" : "}";

			while (!next.SequenceEqual(endToken))
			{
				if (next.SequenceEqual(":"))
				{
					CharSpan identifier = reader.ReadIdentifier();

					ThrowIfTokenMismatch(reader.ReadNext(), "=");
					AddAssignment(node.declarations, identifier);
				}
				else if (next.SequenceEqual(".") && !node.IsRoot)
				{
					CharSpan identifier = reader.ReadIdentifier();

					next = reader.ReadNext();

					if (next.SequenceEqual("=")) AddAssignment(node.assignments, identifier);
					else if (next.SequenceEqual("(")) AddInvocation(identifier);
					else throw UnexpectedTokenException(next);
				}
				else throw UnexpectedTokenException(next);

				next = reader.ReadNext();

				void AddAssignment(Dictionary<string, ArgumentNode> map, CharSpan identifier)
				{
					string converted = new(identifier);

					if (!map.ContainsKey(converted)) map.Add(converted, ArgumentNode.Create(reader, node));
					else throw new FormatException($"Duplicated identifier '{converted}' for assignment.");
				}

				void AddInvocation(CharSpan identifier) => node.invocations.Add(InvocationNode.Create(reader, node));
			}

			return node;
		}
	}

	sealed class InvocationNode : Node
	{
		readonly List<ArgumentNode> arguments = new();

		public static readonly InvocationNode empty = new();

		public Node this[int index] => arguments[index];

		public static InvocationNode Create(SegmentReader reader, ScopeNode scope)
		{
			CharSpan next = reader.PeekNext();

			if (next.SequenceEqual(")"))
			{
				reader.ReadNext();
				return empty;
			}

			InvocationNode node = new();

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
}