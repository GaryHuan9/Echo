using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Echo.Generation;

/// <summary>
/// Source generator for the GeneratedStatisticsAttribute in Echo.Common.Compute.
/// </summary>
[Generator]
public partial class StatisticsGenerator : IIncrementalGenerator
{
	const string MethodName = "Report";
	const string InterfaceName = "IStatistics";
	const string NamespaceName = "Echo.Common.Compute";
	const string AttributeName = "GeneratedStatisticsAttribute";

	const int PackWidth = 4; //How many UInt64s are packed together
	const int LineWidth = 2; //How many packs per 64-byte cache line

	/// <summary>
	/// <see cref="Regex"/> filter allowing only words (A-Z a-z 0-9), space ` `, and slash `/`.
	/// </summary>
	static readonly Regex filter = new(@"^[\w /]+$", RegexOptions.Compiled);

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var wait = new SpinWait();
		while (!Debugger.IsAttached) wait.SpinOnce();

		var types = context.SyntaxProvider.CreateSyntaxProvider
							(
								CouldBeTargetStruct,
								TargetStructOrDefault
							)
						   .Where(static type => type != default)
						   .Collect().Select(DistinctSelector);

		var invocations = context.SyntaxProvider.CreateSyntaxProvider
								  (
									  CouldBeTargetInvocation,
									  TargetInvocationOrDefault
								  )
								 .Where(static invocation => invocation != default)
								 .Collect().Select(InvocationDistributor);

		var labels = types.Combine(invocations).SelectMany(TypeInvocationMerger)
						  .Select((pair, _) => new SymbolPair<StringSet>(pair.Key, pair.Value));

		var packCounts = labels.Select(PackCountSelector);

		context.RegisterSourceOutput(packCounts, Generation.CreateMembersWithoutLabels);
		context.RegisterSourceOutput(labels, Generation.CreateMembersWithLabels);
	}

	static bool CouldBeTargetStruct(SyntaxNode node, CancellationToken token) => node is StructDeclarationSyntax
	{
		AttributeLists.Count: > 0,
		BaseList.Types.Count: > 0
	};

	static FlatSymbol TargetStructOrDefault(GeneratorSyntaxContext context, CancellationToken token)
	{
		var syntax = (StructDeclarationSyntax)context.Node;
		var symbol = context.SemanticModel.GetDeclaredSymbol(syntax, token);
		return IsTargetType(symbol) ? new FlatSymbol(symbol) : default;
	}

	/// <summary>
	/// Whether <paramref name="node"/> might be the invocation
	/// on <see cref="MethodName"/> that we are looking for.
	/// </summary>
	static bool CouldBeTargetInvocation(SyntaxNode node, CancellationToken token) => node is InvocationExpressionSyntax
	{
		Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: MethodName },
		ArgumentList.Arguments.Count: 1
	};

	/// <summary>
	/// Returns either the invocation parameter into <see cref="MethodName"/> if
	/// the node is actually the invocation that we are looking for, or otherwise null.
	/// </summary>
	static SymbolPair<string> TargetInvocationOrDefault(GeneratorSyntaxContext context, CancellationToken token)
	{
		var syntax = (InvocationExpressionSyntax)context.Node;
		var argument = syntax.ArgumentList.Arguments[0].Expression;

		if (!argument.IsKind(SyntaxKind.StringLiteralExpression)) return default;
		var literal = (string)((LiteralExpressionSyntax)argument).Token.Value;
		if (string.IsNullOrWhiteSpace(literal) || !filter.IsMatch(literal)) return default;

		var expression = ((MemberAccessExpressionSyntax)syntax.Expression).Expression;
		var symbol = context.SemanticModel.GetSymbolInfo(expression, token).Symbol;

		if (GetSymbolType(symbol) is not INamedTypeSymbol type || !IsTargetType(type)) return default;

		return new SymbolPair<string>(new FlatSymbol(type), literal);
	}

	static bool IsTargetType(ITypeSymbol symbol)
	{
		return HasTargetAttribute(symbol) && HasTargetInterface(symbol);

		static bool HasTargetAttribute(ISymbol symbol)
		{
			foreach (AttributeData data in symbol.GetAttributes())
			{
				if (data.AttributeClass is not { Name: AttributeName } attribute) continue;
				if (attribute.ContainingNamespace.ToDisplayString() != NamespaceName) continue;

				return true;
			}

			return false;
		}

		static bool HasTargetInterface(ITypeSymbol symbol)
		{
			foreach (INamedTypeSymbol type in symbol.Interfaces)
			{
				if (type.Name != InterfaceName || !type.IsGenericType) continue;
				if (type.ContainingNamespace.ToDisplayString() != NamespaceName) continue;

				return true;
			}

			return false;
		}
	}

	static ITypeSymbol GetSymbolType(ISymbol symbol) => symbol switch
	{
		ILocalSymbol local => local.Type,
		IFieldSymbol field => field.Type,
		IPropertySymbol property => property.Type,
		_ => null
	};

	/// <summary>
	/// Divides while rounding up; only works with positive numbers.
	/// </summary>
	static int CeilingDivide(int value, int divisor) => (value + divisor - 1) / divisor;

	static SymbolSet DistinctSelector(ImmutableArray<FlatSymbol> array, CancellationToken _) => new(array);

	static Dictionary<FlatSymbol, StringSet> InvocationDistributor(ImmutableArray<SymbolPair<string>> invocations, CancellationToken token)
	{
		var map = new Dictionary<FlatSymbol, StringSet>();

		foreach ((FlatSymbol type, string literal) in invocations)
		{
			token.ThrowIfCancellationRequested();

			if (!map.TryGetValue(type, out StringSet set))
			{
				set = new StringSet();
				map.Add(type, set);
			}

			set.Add(literal);
		}

		return map;
	}

	static Dictionary<FlatSymbol, StringSet> TypeInvocationMerger((SymbolSet, Dictionary<FlatSymbol, StringSet>) pair, CancellationToken token)
	{
		(SymbolSet types, Dictionary<FlatSymbol, StringSet> invocations) = pair;

		foreach (FlatSymbol symbol in types)
		{
			token.ThrowIfCancellationRequested();
			if (invocations.ContainsKey(symbol)) continue;
			invocations.Add(symbol, new StringSet());
		}

		return invocations;
	}

	static SymbolPair<int> PackCountSelector(SymbolPair<StringSet> declaration, CancellationToken _) => new(declaration.type, CeilingDivide(declaration.value.Count, PackWidth));

	readonly record struct FlatSymbol
	{
		public FlatSymbol(ISymbol type)
		{
			container = type.ContainingNamespace.ToDisplayString();
			name = type.Name;
		}

		public readonly string container; //The namespace, but the keyword is reserved XD
		public readonly string name;
	}

	readonly record struct SymbolPair<T>(FlatSymbol type, T value)
	{
		public readonly FlatSymbol type = type;
		public readonly T value = value;
	}

	sealed class SymbolSet : EquitableSet<FlatSymbol>
	{
		public SymbolSet(IEnumerable<FlatSymbol> collection) : base(collection) { }
	}

	sealed class StringSet : EquitableSet<string>
	{
		public StringSet() : base(StringComparer.Ordinal) { }
	}

	class EquitableSet<T> : IEquatable<EquitableSet<T>>
	{
		protected EquitableSet(IEqualityComparer<T> comparer)
		{
			set = new HashSet<T>(comparer);
			this.comparer = comparer;
		}

		protected EquitableSet(IEnumerable<T> collection)
		{
			set = new HashSet<T>(collection);
			comparer = set.Comparer;
		}

		readonly HashSet<T> set;
		readonly IEqualityComparer<T> comparer;
		uint totalHash = 0xB706441D;

		public int Count => set.Count;

		public void Add(T item)
		{
			if (!set.Add(item)) return;

			uint hash = (uint)comparer.GetHashCode(item);
			totalHash ^= RotateLeft(hash, set.Count - 1);

			static uint RotateLeft(uint value, int shift) => (value << shift) | (value >> (32 - shift));
		}

		public T[] ToArray() => set.ToArray();

		public bool Equals(EquitableSet<T> other)
		{
			if (other is null) return false;
			if (set == other.set) return true;
			if (set.Count != other.set.Count || totalHash != other.totalHash) return false;

			foreach (T item in other.set)
			{
				if (!set.Contains(item)) return false;
			}

			return true;
		}

		public HashSet<T>.Enumerator GetEnumerator() => set.GetEnumerator();

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((EquitableSet<T>)obj);
		}

		public override int GetHashCode() => throw new NotSupportedException();
	}
}