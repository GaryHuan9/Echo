using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Echo.Generation;

[Generator]
public class StatisticsGenerator : IIncrementalGenerator
{
	const string TargetName = "Report";
	const string TargetContainingTypeName = "Statistics";
	const string TargetNamespace = "Echo.Common.Mathematics";
	const string LabelParameterName = "label";

	static readonly Regex filter = new(@"^[\w :/]+$", RegexOptions.Compiled);

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var expressions = context.SyntaxProvider.CreateSyntaxProvider
		(
			CouldBeTargetInvocation,
			TargetExpressionOrNull
		);

		expressions = expressions.Where(static expression => expression != null);

		var literals = expressions.Where(static expression => expression.IsKind(SyntaxKind.StringLiteralExpression))
								  .Select(static (expression, _) => (string)((LiteralExpressionSyntax)expression).Token.Value)
								  .Where(static literal => !string.IsNullOrWhiteSpace(literal) && filter.IsMatch(literal));
		var invalids = expressions.Where(static expression => !expression.IsKind(SyntaxKind.StringLiteralExpression));

		context.RegisterSourceOutput(literals.Collect(), CreateSource);
		context.RegisterSourceOutput(invalids, CreateDiagnosticErrors);
	}

	static bool CouldBeTargetInvocation(SyntaxNode node, CancellationToken token) => node is InvocationExpressionSyntax
	{
		Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: TargetName },
		ArgumentList.Arguments.Count: 1
	};

	static ExpressionSyntax TargetExpressionOrNull(GeneratorSyntaxContext context, CancellationToken token)
	{
		if (!IsTargetInvocation(context, token)) return null;
		var syntax = (InvocationExpressionSyntax)context.Node;
		return syntax.ArgumentList.Arguments[0].Expression;

		static bool IsTargetInvocation(GeneratorSyntaxContext context, CancellationToken token) =>
			context.SemanticModel.GetSymbolInfo(context.Node, token).Symbol is IMethodSymbol symbol &&
			symbol.ContainingSymbol is INamedTypeSymbol parent && IsTargetContainingType(parent);

		static bool IsTargetContainingType(INamedTypeSymbol symbol) => symbol.Name == TargetContainingTypeName && symbol.ContainingNamespace.ToDisplayString() == TargetNamespace;
	}

	static void CreateSource(SourceProductionContext context, ImmutableArray<string> array)
	{
		ReadOnlySpan<string> literals = SortDistinct(array);
		var builder = new SourceBuilder(nameof(StatisticsGenerator));

		builder.NewSection();
		builder.Using("System");
		builder.Using("System.Runtime.CompilerServices");
		builder.Using("System.Runtime.InteropServices");

		builder.NewSection();
		builder.Namespace(TargetNamespace);

		builder.NewSection();
		builder.Attribute("StructLayout", "LayoutKind.Sequential");
		using (builder.FetchBlock($"partial struct {TargetContainingTypeName}"))
		{
			for (int i = 0; i < literals.Length; i++) builder.Line($"ulong count{i}");

			CreateSourceMethodReport(builder, literals);
		}

		context.AddSource("Statistics.g.cs", builder.ToString());
	}

	/// <summary>
	/// Sorts an <see cref="ImmutableArray{T}"/> of <see cref="string"/>s by the <see cref="string.Length"/>
	/// of the individual <see cref="string"/>s and remove all the duplicated <see cref="string"/>s.
	/// </summary>
	/// <param name="array">The <see cref="ImmutableArray{T}"/> to sort.</param>
	/// <returns>A <see cref="ReadOnlySpan{T}"/> of <see cref="string"/> that contains the result.</returns>
	static ReadOnlySpan<string> SortDistinct(ImmutableArray<string> array)
	{
		string[] result = array.ToArray();
		Array.Sort(result, LengthComparer);

		var set = new HashSet<string>(StringComparer.Ordinal);

		int currentLength = 0;
		int distinctCount = 0;

		foreach (string current in result)
		{
			if (currentLength != current.Length)
			{
				set.Clear();
				currentLength = current.Length;
			}

			if (set.Add(current)) result[distinctCount++] = current;
		}

		return result.AsSpan(0, distinctCount);

		static int LengthComparer(string value0, string value1) => value0.Length.CompareTo(value1.Length);
	}

	static void CreateSourceMethodReport(SourceBuilder builder, ReadOnlySpan<string> literals)
	{
		builder.NewSection();
		builder.Attribute("MethodImpl", "MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization");
		using var _ = builder.FetchBlock($"public partial void {TargetName}(string {LabelParameterName})");

		using (builder.FetchBlock($"switch ({LabelParameterName}.Length)"))
		{
			for (int index = 0; index < literals.Length;)
			{
				string literal = literals[index];
				int length = literal.Length;
				int startIndex = index;

				using (builder.FetchBlock($"case {length}:"))
				{
					do
					{
						if (index > startIndex)
						{
							literal = literals[index];
							if (literal.Length != length) break;
							builder.Prefix("else if (");
						}
						else builder.Prefix("if (");

						for (int i = 0; i < length; i++)
						{
							builder.Append($"{LabelParameterName}[{i}] == '{literal[i]}'");
							if (i + 1 < length) builder.Append(" && ");
						}

						builder.Postfix($") ++count{index}");
					}
					while (++index < literals.Length);

					builder.Line("else break");
					builder.Line("return");
				}
			}
		}

		builder.NewSection();
		builder.Line($"throw new ArgumentOutOfRangeException({LabelParameterName})");
	}

	static void CreateDiagnosticErrors(SourceProductionContext context, ExpressionSyntax syntax)
	{
		// Diagnostic diagnostic = Diagnostic.Create();
		//
		// context.ReportDiagnostic();
	}
}