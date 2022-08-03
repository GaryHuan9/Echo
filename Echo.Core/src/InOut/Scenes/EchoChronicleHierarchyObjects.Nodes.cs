using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Echo.Core.Common;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.InOut.Models;
using Echo.Core.Scenic.Geometries;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.InOut.Scenes;

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
				"\""   => LiteralNode.Create(reader),
				"new"  => TypedNode.Create(reader, stack),
				"link" => FindLink(reader, stack),
				_      => throw reader.UnexpectedTokenException(next)
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

		public static ParametersNode Create(SegmentReader reader, ScopeStack scope)
		{
			CharSpan next = reader.PeekNext();

			if (EqualsSingle(next, ')'))
			{
				reader.ThrowIfNextMismatch(')');
				return empty;
			}

			ParametersNode node = new();

			while (true)
			{
				node.arguments.Add(ArgumentNode.Create(reader, scope));

				next = reader.ReadNext();

				if (EqualsSingle(next, ')')) break;
				reader.ThrowIfTokenMismatch(',', next);
			}

			return node;
		}
	}

	sealed class LiteralNode : ArgumentNode
	{
		LiteralNode(CharSpan content) => this.content = new string(content);

		static LiteralNode()
		{
			AddTryParser<int>(Parsers.TryParse);
			AddTryParser<float>(Parsers.TryParse);
			AddTryParser<Float2>(Parsers.TryParse);
			AddTryParser<Float3>(Parsers.TryParse);
			AddTryParser<Float4>(Parsers.TryParse);
			AddTryParser<Int2>(Parsers.TryParse);
			AddTryParser<Int3>(Parsers.TryParse);
			AddTryParser<Int4>(Parsers.TryParse);
			AddTryParser<RGBA128>(RGBA128.TryParse);
			AddTryParser<RGB128>(RGBA128.TryParse);
			AddPathTryParser<Texture>(path => TextureGrid.Load<RGB128>(path));
			AddPathTryParser<ITriangleSource>(path => new FileTriangleSource(path));
			AddParser(span => span);

			void AddTryParser<T>(TryParser<T> source) => parsers.Add(typeof(T), (TryParser<object>)((CharSpan span, out object result) =>
			{
				bool success = source(span, out T original);
				result = original;
				return success;
			}));

			void AddPathTryParser<T>(Func<string, T> source) => parsers.Add(typeof(T), (PathParser<object>)(path => source(Path.GetFullPath(path))));

			void AddParser<T>(Parser<T> source) => parsers.Add(typeof(T), (Parser<object>)(span => source(span)));
		}

		readonly string content;
		object constructed;

		static readonly Dictionary<Type, object> parsers = new();

		public override Type GetType(EchoChronicleHierarchyObjects main) => null;

		public override object Construct(EchoChronicleHierarchyObjects main, Type targetType)
		{
			if (targetType.HasElementType) targetType = targetType.GetElementType()!;
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
				default: throw new FormatException($"No parser found for literal type '{targetType}'.");
			}

			if (!success) throw new FormatException($"Unable to parse literal string '{content}' to destination type '{targetType}'.");

			return constructed;
		}

		public static LiteralNode Create(SegmentReader reader) => new(reader.ReadUntil('"'));

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
		readonly List<Identified<Node>> children = new();
		object constructed;

		Type _type;

		string TypeString => constructor.identifier;

		public override Type GetType(EchoChronicleHierarchyObjects main) => _type ??= main.typeMap[TypeString] ?? throw new FormatException($"Unrecognized type '{TypeString}'.");

		public override object Construct(EchoChronicleHierarchyObjects main, Type targetType)
		{
			object result = Construct(main);
			Ensure.IsTrue(targetType.IsInstanceOfType(result));
			return result;
		}

		public object Construct(EchoChronicleHierarchyObjects main)
		{
			if (constructed != null) return constructed;

			Type type = GetType(main);

			ReadOnlySpan<ConstructorInfo> candidates = type.GetConstructors();
			ConstructorInfo matched = constructor.node.FirstMatch(main, candidates);

			if (matched == null) throw new FormatException($"No matching constructor for type '{TypeString}'.");

			constructed = matched.Invoke(constructor.node.Construct(main, matched));

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

					node.children.Add(new Identified<Node>(identifier, child));
				}
				else if (EqualsSingle(next, ':'))
				{
					string identifier = reader.ReadIdentifier();
					reader.ThrowIfNextMismatch('=');

					stack.Add(new Identified<ArgumentNode>(identifier, ArgumentNode.Create(reader, stack)));
				}
				else throw reader.UnexpectedTokenException(next);

				next = reader.ReadNext();
			}

			return node;
		}
	}
}