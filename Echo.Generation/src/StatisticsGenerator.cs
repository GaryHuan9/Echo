using System;
using System.Collections.Generic;
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
		var types = context.SyntaxProvider.CreateSyntaxProvider
							(
								CouldBeTargetStruct,
								TargetStructOrNull
							)
						   .Where(static type => type != null);

		var invocations = context.SyntaxProvider.CreateSyntaxProvider
								  (
									  CouldBeTargetInvocation,
									  TargetInvocationOrDefault
								  )
								 .Where(static invocation => invocation.type != null && invocation.literal != null)
								 .Where(static invocation => IsValidLiteral(invocation.literal));

		invocations.Collect().SelectMany(static invocations =>
		{
			var map = new Dictionary<INamedTypeSymbol, HashSet<string>>(SymbolEqualityComparer.Default);

			foreach (Invocation invocation in invocations) { }
		});

		var labels = invocations.Select(static (invocation, _) => ((LiteralExpressionSyntax)invocation.expression).Token.Text)
								.Collect().Select((array, _) => array.Distinct(StringComparer.Ordinal).ToArray());

		var packCount = labels.Select(static (labels, _) => CeilingDivide(labels.Length, PackWidth));

		context.RegisterSourceOutput(packCount, Generation.CreateMembersWithoutLabels);
		context.RegisterSourceOutput(labels, Generation.CreateMembersWithLabels);
	}

	static bool CouldBeTargetStruct(SyntaxNode node, CancellationToken token) => node is StructDeclarationSyntax
	{
		AttributeLists.Count: > 0,
		BaseList.Types.Count: > 0
	};

	static INamedTypeSymbol TargetStructOrNull(GeneratorSyntaxContext context, CancellationToken token)
	{
		var syntax = (StructDeclarationSyntax)context.Node;
		var symbol = context.SemanticModel.GetDeclaredSymbol(syntax, token);
		return IsTargetType(symbol) ? symbol : null;
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
	static Invocation TargetInvocationOrDefault(GeneratorSyntaxContext context, CancellationToken token)
	{
		var syntax = (InvocationExpressionSyntax)context.Node;
		var expression = syntax.ArgumentList.Arguments[0].Expression;

		if (!expression.IsKind(SyntaxKind.StringLiteralExpression)) return default;
		var literal = (string)((LiteralExpressionSyntax)expression).Token.Value;

		if (context.SemanticModel.GetDeclaredSymbol(syntax, token) is not IMethodSymbol symbol) return default;
		if (symbol.ContainingSymbol is not INamedTypeSymbol type || !IsTargetType(type)) return default;

		return new Invocation(type, literal);
	}

	record struct Invocation(INamedTypeSymbol type, string literal);

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

	static bool IsValidLiteral(string literal) => !string.IsNullOrWhiteSpace(literal) && filter.IsMatch(literal);

	/// <summary>
	/// Divides while rounding up; only works with positive numbers.
	/// </summary>
	static int CeilingDivide(int value, int divisor) => (value + divisor - 1) / divisor;
}