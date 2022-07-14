using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Files;

namespace Echo.Core.InOut;

using CharSpan = ReadOnlySpan<char>;

partial class EchoChronicleHierarchyObjects
{
	abstract class Node
	{
		protected static FormatException UnexpectedTokenException(CharSpan token) => new($"Encountered unexpected token: {token}.");

		[DebuggerHidden]
		[StackTraceHidden]
		protected static void ThrowIfTokenMismatch(CharSpan token, CharSpan expected)
		{
			if (token.SequenceEqual(expected)) return;
			throw new FormatException($"Expecting token '{expected}', encountered: '{token}'.");
		}
	}

	abstract class ArgumentNode : Node
	{
		protected abstract Type ExplicitType { get; }

		public abstract object Construct(EchoChronicleHierarchyObjects objects, Type targetType);

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
		readonly List<Identified<TypedNode>> children = new();

		public ReadOnlySpan<Identified<TypedNode>> Children => CollectionsMarshal.AsSpan(children);

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

				ArgumentNode argument = ArgumentNode.Create(reader, stack);
				Identified<ArgumentNode> identified = new(identifier, argument);

				if (argument is TypedNode typed) node.children.Add(new Identified<TypedNode>(identifier, typed));

				stack.Add(identified);
				next = reader.ReadNext();
			}

			return node;
		}
	}

	sealed class ParametersNode : Node
	{
		readonly List<ArgumentNode> arguments = new();

		object[] constructed;

		public static readonly ParametersNode empty = new();

		public Node this[int index] => arguments[index];

		public T FirstMatch<T>(ReadOnlySpan<T> candidates) where T : MethodBase
		{
			foreach (T candidate in candidates)
			{
				ParameterInfo[] parameters = candidate.GetParameters();
				if (parameters.Length != arguments.Count) continue;

				//TODO: more checks with the actual argument
				return candidate;
			}

			return null;
		}

		public object[] Construct(EchoChronicleHierarchyObjects objects, MethodBase method)
		{
			if (constructed != null) return constructed;

			int count = arguments.Count;

			if (count == 0)
			{
				constructed = Array.Empty<object>();
				return constructed;
			}

			constructed = new object[count];
			var parameters = method.GetParameters();
			Assert.AreEqual(count, parameters.Length);

			for (int i = 0; i < count; i++)
			{
				Type type = parameters[i].ParameterType;
				constructed[i] = arguments[i].Construct(objects, type);
			}

			return constructed;
		}

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

		public override object Construct(EchoChronicleHierarchyObjects objects, Type targetType) => throw new NotImplementedException();

		public static LiteralNode Create(SegmentReader reader)
		{
			// CharSpan next = reader.PeekNext();
			//TODO: handle explicitly defined types using 'next'
			//note that the returned span can change if we invoke ReadNext again

			return new(reader.ReadUntil('`'));
		}
	}

	sealed class TypedNode : ArgumentNode
	{
		TypedNode(Identified<ParametersNode> constructor) => this.constructor = constructor;

		readonly Identified<ParametersNode> constructor;
		readonly List<Identified<Node>> children = new();
		object constructed;

		Type _type;

		string TypeString => constructor.identifier;

		public Type GetType(TypeMap map) => _type ??= map[TypeString] ?? throw new FormatException($"Unrecognized type '{TypeString}'.");

		public override object Construct(EchoChronicleHierarchyObjects objects, Type targetType)
		{
			if (constructed != null) return constructed;

			Type type = GetType(objects.typeMap);
			Assert.AreEqual(type, targetType);

			ReadOnlySpan<ConstructorInfo> candidates = type.GetConstructors();
			ConstructorInfo matched = constructor.node.FirstMatch(candidates);

			if (matched == null) throw new FormatException($"No matching constructor for type '{TypeString}'.");

			constructed = matched.Invoke(constructor.node.Construct(objects, matched));

			foreach ((string identifier, Node node) in children)
			{
				switch (node)
				{
					case ParametersNode parameters:
					{
						//Method invocation

						type.GetMethod()
						break;
					}
					case ArgumentNode argument:
					{
						//Property assignment
						PropertyInfo property = type.GetProperty(identifier);
						property.SetValue(constructed,);
						break;
					}
					default:
				}
			}

			return constructed;
		}

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
}