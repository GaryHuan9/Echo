using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Echo.Core.Common.Memory;

namespace Echo.Core.InOut.EchoDescription;

using CharSpan = ReadOnlySpan<char>;

abstract class Node { }

abstract class ArgumentNode : Node
{
	public abstract Type GetConstructType();

	public abstract object Construct(EchoSource main, Type targetType);

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

	public object[] Construct(EchoSource main, MethodBase targetMethod)
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

	public T FirstMatch<T>(ReadOnlySpan<T> methods) where T : MethodBase
	{
		foreach (T method in methods)
		{
			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length < arguments.Count) continue;

			bool needAttribute = !method.IsConstructor || parameters.Length > 0; //Parameterless constructor are always included
			// if (parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom())
			if (needAttribute && method.GetCustomAttribute<EchoSourceUsableAttribute>() == null) continue;

			for (int i = 0; i < parameters.Length; i++)
			{
				ParameterInfo parameter = parameters[i];

				if (i < arguments.Count)
				{
					Type argument = arguments[i].GetConstructType();
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

		if (SegmentReader.EqualsSingle(next, ')'))
		{
			reader.ThrowIfNextMismatch(')');
			return empty;
		}

		using var _ = stack.Advance();
		var node = new ParametersNode();

		do
		{
			if (SegmentReader.EqualsSingle(next, ':'))
			{
				reader.ThrowIfNextMismatch(':');
				stack.AddNext(reader);
			}
			else node.arguments.Add(ArgumentNode.Create(reader, stack));

			next = reader.PeekNext();
		}
		while (!SegmentReader.EqualsSingle(next, ')'));

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

	readonly string content;
	readonly int lineNumber;
	object constructed;

	public override Type GetConstructType() => null;

	public override object Construct(EchoSource main, Type targetType)
	{
		if (constructed != null && targetType.IsInstanceOfType(constructed)) return constructed;

		bool success;

		switch (LiteralParser.TryGetParser(targetType))
		{
			case LiteralParser.TryParser<object> parser:
			{
				success = parser(content, out constructed);
				break;
			}
			case LiteralParser.PathParser<object> parser:
			{
				constructed = parser(Path.Join(main.currentDirectory, content));
				success = true;
				break;
			}
			case LiteralParser.Parser<object> parser:
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
}

sealed class TypedNode : ArgumentNode
{
	TypedNode(Identified<ParametersNode> constructor) => this.constructor = constructor;

	readonly Identified<ParametersNode> constructor;
	readonly List<Identified<Node>> members = new();
	object constructed;

	Type _type;

	string TypeString => constructor.identifier;

	public override Type GetConstructType() => _type ??= TypeMap.Find(TypeString) ?? throw new FormatException($"Unrecognized type '{TypeString}'.");

	public override object Construct(EchoSource main, Type targetType)
	{
		object result = Construct(main);
		if (targetType.IsInstanceOfType(result)) return result;
		throw new FormatException($"Cannot assign an object of type '{TypeString}' to type '{targetType}'.");
	}

	public object Construct(EchoSource main)
	{
		if (constructed != null) return constructed;

		Type type = GetConstructType();

		ReadOnlySpan<ConstructorInfo> candidates = type.GetConstructors();
		ConstructorInfo matched = constructor.node.FirstMatch(candidates);

		if (matched != null) constructed = matched.Invoke(constructor.node.Construct(main, matched));
		else throw new FormatException($"No matching constructor for type '{TypeString}'.");

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

					MethodInfo selected = parameters.FirstMatch<MethodInfo>(fill.Filled);
					selected?.Invoke(constructed, parameters.Construct(main, selected));

					if (selected == null) throw new FormatException($"No matching method named '{identifier}' on type '{TypeString}'.");
					break;
				}
				case ArgumentNode argument:
				{
					//Property assignment
					PropertyInfo property = type.GetProperty(identifier);
					if (property?.GetCustomAttribute<EchoSourceUsableAttribute>() == null) property = null;

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

		if (SegmentReader.EqualsSingle(next, '('))
		{
			reader.ThrowIfNextMismatch('(');
			parameters = ParametersNode.Create(reader, stack);
			next = reader.PeekNext();
		}
		else parameters = ParametersNode.empty;

		TypedNode node = new(new Identified<ParametersNode>(type, parameters));

		if (!SegmentReader.EqualsSingle(next, '{')) return node;

		using var _ = stack.Advance();

		reader.ThrowIfNextMismatch('{');
		next = reader.ReadNext();

		while (!SegmentReader.EqualsSingle(next, '}'))
		{
			if (SegmentReader.EqualsSingle(next, '.'))
			{
				string identifier = reader.ReadIdentifier();
				Node child;

				next = reader.ReadNext();

				if (SegmentReader.EqualsSingle(next, '=')) child = ArgumentNode.Create(reader, stack);
				else if (SegmentReader.EqualsSingle(next, '(')) child = ParametersNode.Create(reader, stack);
				else throw reader.UnexpectedTokenException(next);

				node.members.Add(new Identified<Node>(identifier, child));
			}
			else if (SegmentReader.EqualsSingle(next, ':')) stack.AddNext(reader);
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

	public override Type GetConstructType() => null;

	public override object Construct(EchoSource main, Type targetType)
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

		while (!SegmentReader.EqualsSingle(next, ']'))
		{
			if (SegmentReader.EqualsSingle(next, ':'))
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