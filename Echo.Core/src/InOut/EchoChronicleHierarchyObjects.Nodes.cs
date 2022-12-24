using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Echo.Core.Common;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.InOut;

using CharSpan = ReadOnlySpan<char>;

partial class EchoChronicleHierarchyObjects
{
	abstract class Node { }

	abstract class ArgumentNode : Node
	{
		public abstract Type GetType(EchoChronicleHierarchyObjects main);

		public abstract object Construct(EchoChronicleHierarchyObjects main, Type targetType);

		public static ArgumentNode Create(SegmentReader reader, ScopeStack stack)
		{
			CharSpan next = reader.ReadNext();

			return next.ToString() switch //OPTIMIZE: use https://github.com/dotnet/csharplang/issues/1881 when we switch to dotnet 7 
			{
				"\"" => LiteralNode.Create(reader),
				"new" => TypedNode.Create(reader, stack),
				"link" => FindLink(reader, stack),
				"[" => ArrayNode.Create(reader, stack),
				_ => throw reader.UnexpectedTokenException(next)
			};

			static ArgumentNode FindLink(SegmentReader reader, ScopeStack stack)
			{
				string identifier = reader.ReadIdentifier();
				ArgumentNode node = stack.Find(identifier);
				if (node != null) return node;

				throw new FormatException($"Cannot find object in scope with identifier '{identifier}' on line {reader.CurrentLine}.");
			}
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
				reader.ThrowIfTokenMismatch(':', next);
				string identifier = reader.ReadIdentifier();
				reader.ThrowIfNextMismatch('=');

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

		public object[] Construct(EchoChronicleHierarchyObjects main, MethodBase targetMethod)
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
					if (type.IsByRef) type = type.GetElementType();
					argument = arguments[i].Construct(main, type);
				}
				else argument = Type.Missing; //For optional parameters
			}

			return constructed;
		}

		public T FirstMatch<T>(EchoChronicleHierarchyObjects main, ReadOnlySpan<T> methods) where T : MethodBase
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
						Type argument = arguments[i].GetType(main);
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

		public static ParametersNode Create(SegmentReader reader, ScopeStack stack)
		{
			CharSpan next = reader.PeekNext();

			if (EqualsSingle(next, ')'))
			{
				reader.ThrowIfNextMismatch(')');
				return empty;
			}

			using var _ = stack.Advance();
			var node = new ParametersNode();

			do
			{
				if (EqualsSingle(next, ':'))
				{
					reader.ThrowIfNextMismatch(':');
					stack.AddNext(reader);
				}
				else node.arguments.Add(ArgumentNode.Create(reader, stack));

				next = reader.PeekNext();
			}
			while (!EqualsSingle(next, ')'));

			reader.ThrowIfNextMismatch(')');

			return node;
		}
	}

	sealed class LiteralNode : ArgumentNode
	{
		LiteralNode(CharSpan content, int lineNumber)
		{
			this.content = new string(content);
			this.lineNumber = lineNumber;
		}

		static LiteralNode()
		{
			AddTryParser<bool>(bool.TryParse);
			AddTryParser<int>(InvariantFormat.TryParse);
			AddTryParser<float>(InvariantFormat.TryParse);
			AddTryParser<Float2>(InvariantFormat.TryParse);
			AddTryParser<Float3>(InvariantFormat.TryParse);
			AddTryParser<Float4>(InvariantFormat.TryParse);
			AddTryParser<Int2>(InvariantFormat.TryParse);
			AddTryParser<Int3>(InvariantFormat.TryParse);
			AddTryParser<Int4>(InvariantFormat.TryParse);
			AddTryParser<RGBA128>(RGBA128.TryParse);
			AddTryParser<RGB128>(RGBA128.TryParse);
			AddPathTryParser<Texture>(path => TextureGrid.Load<RGB128>(path));
			AddPathTryParser(path => new Mesh(path));
			AddParser(span => span);

			void AddTryParser<T>(TryParser<T> source) => parsers.Add(
				typeof(T), (TryParser<object>)((CharSpan span, out object result) =>
												  {
													  bool success = source(span, out T original);
													  result = original;
													  return success;
												  })
			);

			void AddPathTryParser<T>(Func<string, T> source) => parsers.Add(typeof(T), (PathParser<object>)(path => source(Path.GetFullPath(path))));

			void AddParser<T>(Parser<T> source) => parsers.Add(typeof(T), (Parser<object>)(span => source(span)));
		}

		readonly string content;
		readonly int lineNumber;
		object constructed;

		static readonly Dictionary<Type, object> parsers = new();

		public override Type GetType(EchoChronicleHierarchyObjects main) => null;

		public override object Construct(EchoChronicleHierarchyObjects main, Type targetType)
		{
			if (constructed != null && targetType.IsInstanceOfType(constructed)) return constructed;

			bool success;

			switch (parsers.TryGetValue(targetType))
			{
				case TryParser<object> parser:
				{
					success = parser(content, out constructed);
					break;
				}
				case PathParser<object> parser:
				{
					constructed = parser(Path.Join(main.directory, content));
					success = true;
					break;
				}
				case Parser<object> parser:
				{
					constructed = parser(content);
					success = true;
					break;
				}
				default: throw new FormatException($"No parser found for literal type '{targetType}' on line {lineNumber}.");
			}

			if (!success) throw new FormatException($"Unable to parse literal string '{content}' to type '{targetType}' on line {lineNumber}.");

			return constructed;
		}

		public static LiteralNode Create(SegmentReader reader)
		{
			int lineNumber = reader.CurrentLine;
			return new LiteralNode(reader.ReadUntil('"'), lineNumber);
		}

		// ReSharper disable TypeParameterCanBeVariant

		delegate bool TryParser<T>(CharSpan span, out T result);
		delegate T PathParser<T>(string path);
		delegate T Parser<T>(string span);

		// ReSharper restore TypeParameterCanBeVariant
	}

	sealed class TypedNode : ArgumentNode
	{
		TypedNode(Identified<ParametersNode> constructor) => this.constructor = constructor;

		readonly Identified<ParametersNode> constructor;
		readonly List<Identified<Node>> members = new();
		object constructed;

		Type _type;

		string TypeString => constructor.identifier;

		public override Type GetType(EchoChronicleHierarchyObjects main) => _type ??= main.typeMap[TypeString] ?? throw new FormatException($"Unrecognized type '{TypeString}'.");

		public override object Construct(EchoChronicleHierarchyObjects main, Type targetType)
		{
			object result = Construct(main);
			if (targetType.IsInstanceOfType(result)) return result;
			throw new FormatException($"Cannot assign an object of type '{TypeString}' to type '{targetType}'.");
		}

		public object Construct(EchoChronicleHierarchyObjects main)
		{
			if (constructed != null) return constructed;

			Type type = GetType(main);

			ReadOnlySpan<ConstructorInfo> candidates = type.GetConstructors();
			ConstructorInfo matched = constructor.node.FirstMatch(main, candidates);

			if (matched == null) throw new FormatException($"No matching constructor for type '{TypeString}'.");

			constructed = matched.Invoke(constructor.node.Construct(main, matched));

			foreach ((string identifier, Node node) in members)
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

						MethodInfo selected = parameters.FirstMatch<MethodInfo>(main, fill.Filled);

						if (selected == null) throw new FormatException($"No matching method named '{identifier}' on type '{TypeString}'.");

						selected.Invoke(constructed, parameters.Construct(main, selected));

						break;
					}
					case ArgumentNode argument:
					{
						//Property assignment
						PropertyInfo property = type.GetProperty(identifier);
						MethodInfo setter = property?.GetSetMethod();

						if (setter != null) setter.Invoke(constructed, new[] { argument.Construct(main, property.PropertyType) });
						else throw new FormatException($"No setter found for property named '{identifier}' on type '{TypeString}'.");

						break;
					}
					default: throw new NotSupportedException(); //OPTIMIZE use UnreachableException
				}
			}

			return constructed;
		}

		new public static TypedNode Create(SegmentReader reader, ScopeStack stack)
		{
			string type = reader.ReadIdentifier();
			CharSpan next = reader.PeekNext();
			ParametersNode parameters;

			if (EqualsSingle(next, '('))
			{
				reader.ThrowIfNextMismatch('(');
				parameters = ParametersNode.Create(reader, stack);
				next = reader.PeekNext();
			}
			else parameters = ParametersNode.empty;

			TypedNode node = new(new Identified<ParametersNode>(type, parameters));

			if (!EqualsSingle(next, '{')) return node;

			using var _ = stack.Advance();

			reader.ThrowIfNextMismatch('{');
			next = reader.ReadNext();

			while (!EqualsSingle(next, '}'))
			{
				if (EqualsSingle(next, '.'))
				{
					string identifier = reader.ReadIdentifier();
					Node child;

					next = reader.ReadNext();

					if (EqualsSingle(next, '=')) child = ArgumentNode.Create(reader, stack);
					else if (EqualsSingle(next, '(')) child = ParametersNode.Create(reader, stack);
					else throw reader.UnexpectedTokenException(next);

					node.members.Add(new Identified<Node>(identifier, child));
				}
				else if (EqualsSingle(next, ':')) stack.AddNext(reader);
				else throw reader.UnexpectedTokenException(next);

				next = reader.ReadNext();
			}

			return node;
		}
	}

	sealed class ArrayNode : ArgumentNode
	{
		ArrayNode(int lineNumber) => this.lineNumber = lineNumber;

		readonly List<ArgumentNode> items = new();
		readonly int lineNumber;

		object constructed; //An ImmutableArray<> of some type

		public override Type GetType(EchoChronicleHierarchyObjects main) => null;

		public override object Construct(EchoChronicleHierarchyObjects main, Type targetType)
		{
			if (constructed?.GetType() == targetType) return constructed;
			Type elementType = GetElementType(targetType);

			var array = Array.CreateInstance(elementType, items.Count);
			const BindingFlags Binding = BindingFlags.NonPublic | BindingFlags.Instance;

			for (int i = 0; i < items.Count; i++) array.SetValue(items[i].Construct(main, elementType), i);
			var constructor = targetType.GetConstructor(Binding, new[] { elementType.MakeArrayType() });

			constructed = constructor!.Invoke(new[] { (object)array });
			return constructed;
		}

		Type GetElementType(Type targetType)
		{
			if (targetType.GetGenericTypeDefinition() == typeof(ImmutableArray<>)) return targetType.GetGenericArguments()[0];
			throw new FormatException($"Cannot assign an {typeof(ImmutableArray<>)} to type '{targetType}' on line {lineNumber}.");
		}

		new public static ArrayNode Create(SegmentReader reader, ScopeStack stack)
		{
			var node = new ArrayNode(reader.CurrentLine);

			using var _ = stack.Advance();
			CharSpan next = reader.PeekNext();

			while (!EqualsSingle(next, ']'))
			{
				if (EqualsSingle(next, ':'))
				{
					reader.ThrowIfNextMismatch(':');
					stack.AddNext(reader);
				}
				else node.items.Add(ArgumentNode.Create(reader, stack));

				next = reader.PeekNext();
			}

			reader.ThrowIfNextMismatch(']');

			return node;
		}
	}
}