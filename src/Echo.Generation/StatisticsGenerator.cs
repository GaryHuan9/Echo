using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Echo.Generation;

/// <summary>
/// Source generator for the GeneratedStatisticsAttribute in Echo.Common.Compute; all structs that
/// implements the IStatistics interface and are attributed with the GeneratedStatisticsAttribute
/// will be candidates for generation.
///
/// The generation will also look for invocations to the Report method with constant string literals
/// as parameters for the different statistics types. At then end, it will generate two files for each
/// candidate struct, one containing members relating only to the numerical fields, and the other
/// creates members relating to the main Report method that is specially treated for the Jitter.
/// </summary>
[Generator]
public partial class StatisticsGenerator : IIncrementalGenerator
{
	const string MethodName = "Report";
	const string InterfaceName = "IStatistics";
	const string NamespaceName = "Echo.Core.Common.Compute.Statistics";
	const string AttributeName = "GeneratedStatisticsAttribute";

	/// <summary>
	/// <see cref="Regex"/> filter allowing only words (A-Z a-z 0-9), space ` `, and slash `/`.
	/// </summary>
	static readonly Regex filter = new(@"^[\w /]+$", RegexOptions.Compiled);

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		//The candidate struct types that we found
		var types = context.SyntaxProvider.CreateSyntaxProvider
							(
								CouldBeTargetStruct,
								TargetStructOrDefault
							)
						   .Where(static type => type != default)
						   .Collect().Select(DistinctSelector);

		//The invocations into the Report method
		var invocations = context.SyntaxProvider.CreateSyntaxProvider
								  (
									  CouldBeTargetInvocation,
									  TargetInvocationOrDefault
								  )
								 .Where(static invocation => invocation != default)
								 .Collect().Select(InvocationDistributor);

		//Combine the two pipelines into one with the invocations paired with the type
		var labels = types.Combine(invocations).SelectMany(TypeInvocationMerger)
						  .Select((pair, _) => new TypePair<StringSet>(pair.Key, pair.Value));

		//Create another pipeline with the divided invocation count for reduced field generation
		var packCounts = labels.Select(PackCountDivider);

		//Generate sources from the pipelines
		context.RegisterSourceOutput(packCounts, Generation.CreateMembersWithoutLabels);
		context.RegisterSourceOutput(labels, Generation.CreateMembersWithLabels);
	}

	/// <summary>
	/// Whether <paramref name="node"/> could be a declaration of a struct type that matches our criteria.
	/// </summary>
	static bool CouldBeTargetStruct(SyntaxNode node, CancellationToken token) => node is StructDeclarationSyntax
	{
		AttributeLists.Count: > 0,
		BaseList.Types.Count: > 0
	};

	/// <summary>
	/// Returns the struct <see cref="FlatType"/> if the declaration is 
	/// </summary>
	static FlatType TargetStructOrDefault(GeneratorSyntaxContext context, CancellationToken token)
	{
		var syntax = (StructDeclarationSyntax)context.Node;
		var symbol = context.SemanticModel.GetDeclaredSymbol(syntax, token);
		return IsTargetType(symbol) ? new FlatType(symbol) : default;
	}

	/// <summary>
	/// Whether <paramref name="node"/> might be the invocation on <see cref="MethodName"/> that we are looking for.
	/// </summary>
	static bool CouldBeTargetInvocation(SyntaxNode node, CancellationToken token) => node is InvocationExpressionSyntax
	{
		Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: MethodName },
		ArgumentList.Arguments.Count: 1 or 2
	};

	/// <summary>
	/// Returns either the invocation parameter into <see cref="MethodName"/> if
	/// the node is actually the invocation that we are looking for, or otherwise null.
	/// </summary>
	static TypePair<string> TargetInvocationOrDefault(GeneratorSyntaxContext context, CancellationToken token)
	{
		var syntax = (InvocationExpressionSyntax)context.Node;
		var argument = syntax.ArgumentList.Arguments[0].Expression;

		if (!argument.IsKind(SyntaxKind.StringLiteralExpression)) return default;
		var literal = (string)((LiteralExpressionSyntax)argument).Token.Value;
		if (string.IsNullOrWhiteSpace(literal) || !filter.IsMatch(literal)) return default;

		var expression = ((MemberAccessExpressionSyntax)syntax.Expression).Expression;
		var symbol = context.SemanticModel.GetSymbolInfo(expression, token).Symbol;

		if (GetSymbolType(symbol) is not INamedTypeSymbol type || !IsTargetType(type)) return default;
		return new TypePair<string>(new FlatType(type), literal);

		static ITypeSymbol GetSymbolType(ISymbol symbol) => symbol switch
		{
			ILocalSymbol local         => local.Type,
			IFieldSymbol field         => field.Type,
			IPropertySymbol property   => property.Type,
			IParameterSymbol parameter => parameter.Type,
			_                          => null
		};
	}

	/// <summary>
	/// Returns whether <paramref name="symbol"/> is a type that matches our criteria for generation.
	/// </summary>
	static bool IsTargetType(INamedTypeSymbol symbol)
	{
		return HasTargetAttribute(symbol) && HasTargetInterface(symbol);

		static bool HasTargetAttribute(INamedTypeSymbol symbol)
		{
			foreach (AttributeData data in symbol.GetAttributes())
			{
				if (data.AttributeClass is not { Name: AttributeName } attribute) continue;
				if (attribute.ContainingNamespace.ToDisplayString() != NamespaceName) continue;

				return true;
			}

			return false;
		}

		static bool HasTargetInterface(INamedTypeSymbol symbol)
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

	/// <summary>
	/// Creates a <see cref="TypeSet"/> from an <see cref="ImmutableArray{T}"/> of <see cref="FlatType"/>.
	/// </summary>
	static TypeSet DistinctSelector(ImmutableArray<FlatType> array, CancellationToken _) => new(array);

	/// <summary>
	/// Distributes invocations based on the type that they are invoking into. 
	/// </summary>
	static Dictionary<FlatType, StringSet> InvocationDistributor(ImmutableArray<TypePair<string>> invocations, CancellationToken token)
	{
		var map = new Dictionary<FlatType, StringSet>();

		foreach ((FlatType type, string literal) in invocations)
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

	/// <summary>
	/// Merges all struct types with their corresponding invocations. If no invocation
	/// is present for a type, then a new empty entry is created for that type. 
	/// </summary>
	static Dictionary<FlatType, StringSet> TypeInvocationMerger((TypeSet, Dictionary<FlatType, StringSet>) pair, CancellationToken token)
	{
		(TypeSet types, Dictionary<FlatType, StringSet> invocations) = pair;

		foreach (FlatType symbol in types)
		{
			token.ThrowIfCancellationRequested();
			if (invocations.ContainsKey(symbol)) continue;
			invocations.Add(symbol, new StringSet());
		}

		return invocations;
	}

	/// <summary>
	/// Ceiling divides the number of invocations.
	/// </summary>
	static TypePair<int> PackCountDivider(TypePair<StringSet> declaration, CancellationToken _) => new(declaration.Type, CeilingDivide(declaration.Value.Count, PackWidth));

	/// <summary>
	/// Divides while rounding up; only works with positive numbers.
	/// </summary>
	static int CeilingDivide(int value, int divisor) => (value + divisor - 1) / divisor;

	/// <summary>
	/// A flattened version of a <see cref="ITypeSymbol"/>, stores only the namespace and the type name.
	/// This allows two <see cref="ITypeSymbol"/> from different compilations to be compared and reused.
	/// </summary>
	readonly record struct FlatType
	{
		public FlatType(ITypeSymbol type)
		{
			Namespace = type.ContainingNamespace.ToDisplayString();
			TypeName = type.Name;
		}

		public readonly string Namespace;
		public readonly string TypeName;
	}

	/// <summary>
	/// A generic pair mapping from a <see cref="FlatType"/> to a <see cref="Value"/> of type <see cref="T"/>.
	/// </summary>
	readonly record struct TypePair<T>(FlatType Type, T Value)
	{
		public readonly FlatType Type = Type;
		public readonly T Value = Value;
	}

	/// <summary>
	/// An <see cref="EquitableSet{T}"/> of <see cref="FlatType"/>s.
	/// </summary>
	sealed class TypeSet : EquitableSet<FlatType>
	{
		public TypeSet(IEnumerable<FlatType> collection) : base(collection) { }
	}

	/// <summary>
	/// An <see cref="EquitableSet{T}"/> of <see cref="string"/>s.
	/// </summary>
	sealed class StringSet : EquitableSet<string>
	{
		public StringSet() : base(StringComparer.Ordinal) { }
	}

	/// <summary>
	/// A hash set that can be used to compare content equality.
	/// Used for better caching between different compilations.
	/// </summary>
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