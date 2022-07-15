using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.Core.Common.Memory;
using Echo.Core.Textures.Colors;

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
		public abstract Type GetType(EchoChronicleHierarchyObjects objects);

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

		public object[] Construct(EchoChronicleHierarchyObjects objects, MethodBase targetMethod)
		{
			if (constructed != null) return constructed;
			var parameters = targetMethod.GetParameters();

			if (parameters.Length == 0)
			{
				constructed = Array.Empty<object>();
				return constructed;
			}

			constructed = new object[parameters.Length];

			for (int i = 0; i < parameters.Length; i++)
			{
				ref object argument = ref constructed[i];

				if (i < arguments.Count)
				{
					Type type = parameters[i].ParameterType;
					argument = arguments[i].Construct(objects, type);
				}
				else argument = Type.Missing; //For optional parameters
			}

			return constructed;
		}

		public T FirstMatch<T>(EchoChronicleHierarchyObjects objects, ReadOnlySpan<T> methods) where T : MethodBase
		{
			foreach (T method in methods)
			{
				ParameterInfo[] parameters = method.GetParameters();
				if (parameters.Length < arguments.Count) goto next;

				for (int i = 0; i < parameters.Length; i++)
				{
					ParameterInfo parameter = parameters[i];

					if (i < arguments.Count)
					{
						Type argument = arguments[i].GetType(objects);
						if (argument != null && !parameter.ParameterType.IsAssignableFrom(argument)) goto next;
					}
					else if (!parameter.IsOptional) goto next;
				}

				return method;

			next:
				{ }
			}

			return null;
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

		readonly string content;
		object constructed;

		static readonly Dictionary<Type, TryParser<object>> parsers = new()
		{
			{ typeof(int), ConvertTryParser<int>(Parsers.TryParse) },
			{ typeof(float), ConvertTryParser<float>(Parsers.TryParse) },
			{ typeof(Float2), ConvertTryParser<Float2>(Parsers.TryParse) },
			{ typeof(Float3), ConvertTryParser<Float3>(Parsers.TryParse) },
			{ typeof(Float4), ConvertTryParser<Float4>(Parsers.TryParse) },
			{ typeof(Int2), ConvertTryParser<Int2>(Parsers.TryParse) },
			{ typeof(Int3), ConvertTryParser<Int3>(Parsers.TryParse) },
			{ typeof(Int4), ConvertTryParser<Int4>(Parsers.TryParse) },
			{ typeof(RGBA128), ConvertTryParser<RGBA128>(RGBA128.TryParse) },
			{ typeof(RGB128), ConvertTryParser<RGB128>(RGBA128.TryParse) }
		};

		public override Type GetType(EchoChronicleHierarchyObjects objects) => null;

		public override object Construct(EchoChronicleHierarchyObjects objects, Type targetType)
		{
			if (targetType.HasElementType) targetType = targetType.GetElementType()!;

			if (constructed != null)
			{
				Assert.AreEqual(constructed.GetType(), targetType);
				return constructed;
			}

			if (!parsers.TryGetValue(targetType, out TryParser<object> parser)) throw new FormatException($"No parser found for literal type '{targetType}'.");
			if (!parser(content, out constructed)) throw new FormatException($"Unable to parse literal string '{content}' to destination type '{targetType}'.");

			return constructed;
		}

		public static LiteralNode Create(SegmentReader reader)
		{
			// CharSpan next = reader.PeekNext();
			//TODO: handle explicitly defined types using 'next'
			//note that the returned span can change if we invoke ReadNext again

			return new LiteralNode(reader.ReadUntil('`'));
		}

		static TryParser<object> ConvertTryParser<T>(TryParser<T> source) => (CharSpan span, out object result) =>
		{
			bool success = source(span, out T original);
			result = original;
			return success;
		};

		delegate bool TryParser<T>(CharSpan span, out T result);
	}

	sealed class TypedNode : ArgumentNode
	{
		TypedNode(Identified<ParametersNode> constructor) => this.constructor = constructor;

		readonly Identified<ParametersNode> constructor;
		readonly List<Identified<Node>> children = new();
		object constructed;

		Type _type;

		string TypeString => constructor.identifier;

		public override Type GetType(EchoChronicleHierarchyObjects objects) => _type ??= objects.typeMap[TypeString] ?? throw new FormatException($"Unrecognized type '{TypeString}'.");

		public override object Construct(EchoChronicleHierarchyObjects objects, Type targetType)
		{
			object result = Construct(objects);
			Assert.IsTrue(targetType.IsInstanceOfType(result));
			return result;
		}

		public object Construct(EchoChronicleHierarchyObjects objects)
		{
			if (constructed != null) return constructed;

			Type type = GetType(objects);

			ReadOnlySpan<ConstructorInfo> candidates = type.GetConstructors();
			ConstructorInfo matched = constructor.node.FirstMatch(objects, candidates);

			if (matched == null) throw new FormatException($"No matching constructor for type '{TypeString}'.");

			constructed = matched.Invoke(constructor.node.Construct(objects, matched));

			foreach ((string identifier, Node node) in children)
			{
				switch (node)
				{
					case ParametersNode parameters:
					{
						//Method invocation
						MethodInfo[] methods = type.GetMethods();
						SpanFill<MethodInfo> fill = methods;

						foreach (MethodInfo method in methods)
						{
							if (method.Name == identifier) fill.Add(method);
						}

						MethodInfo selected = parameters.FirstMatch<MethodInfo>(objects, fill.Filled);

						if (selected == null) throw new FormatException($"No matching method named '{identifier}' on type '{TypeString}'.");

						selected.Invoke(constructed, parameters.Construct(objects, selected));

						break;
					}
					case ArgumentNode argument:
					{
						//Property assignment
						PropertyInfo property = type.GetProperty(identifier);
						MethodInfo setter = property?.GetSetMethod();

						if (setter != null) setter.Invoke(constructed, new[] { argument.Construct(objects, property.PropertyType) });
						else throw new FormatException($"No setter found for property named '{identifier}' on type '{TypeString}'.");

						break;
					}
					default: throw new NotSupportedException();
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

			if (!next.SequenceEqual("{")) return node;

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

			return node;
		}
	}
}