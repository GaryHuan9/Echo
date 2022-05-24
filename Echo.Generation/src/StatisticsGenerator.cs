using System;
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
	const string ReportMethodName = "Report";
	const string StatisticsTypeName = "Statistics";
	const string NamespaceName = "Echo.Common.Mathematics";
	const string LabelParameterName = "label";

	const int PackWidth = 4; //How many UInt64s are packed together
	const int LineWidth = 2; //How many packs per 64-byte cache line

	static readonly Regex filter = new(@"^[\w :/]+$", RegexOptions.Compiled);

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var expressions = context.SyntaxProvider.CreateSyntaxProvider
		(
			CouldBeTargetInvocation,
			TargetExpressionOrNull
		);

		expressions = expressions.Where(static expression => expression != null)
								 .Where(static expression => expression.IsKind(SyntaxKind.StringLiteralExpression));

		var literals = expressions.Select(static (expression, _) => (string)((LiteralExpressionSyntax)expression).Token.Value)
								  .Where(static literal => !string.IsNullOrWhiteSpace(literal) && filter.IsMatch(literal))
								  .Collect().Select((array, _) => array.Distinct().ToArray());

		var packCount = literals.Select(static (literals, _) => CeilingDivide(literals.Length, PackWidth));

		context.RegisterSourceOutput(packCount, CreateFields);
		context.RegisterSourceOutput(literals, CreateMethods);
	}

	static bool CouldBeTargetInvocation(SyntaxNode node, CancellationToken token) => node is InvocationExpressionSyntax
	{
		Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: ReportMethodName },
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

		static bool IsTargetContainingType(INamedTypeSymbol symbol) => symbol.Name == StatisticsTypeName && symbol.ContainingNamespace.ToDisplayString() == NamespaceName;
	}

	static int CeilingDivide(int value, int divisor) => (value + divisor - 1) / divisor;

	static void CreateFields(SourceProductionContext context, int packCount)
	{
		var builder = new SourceBuilder(nameof(StatisticsGenerator));

		builder.NewSection();
		builder.Using("System");
		builder.Using("System.Runtime.CompilerServices");
		builder.Using("System.Runtime.InteropServices");
		builder.Using("System.Runtime.Intrinsics");
		builder.Using("System.Runtime.Intrinsics.X86");

		builder.NewSection();
		builder.Namespace(NamespaceName);

		builder.NewSection();
		int ulongCount = CeilingDivide(packCount, LineWidth) * PackWidth * LineWidth;
		int structSize = Math.Max(ulongCount * sizeof(ulong), 1);
		builder.Attribute("StructLayout", $"LayoutKind.Sequential, Size = {structSize}");
		using (builder.FetchBlock($"partial struct {StatisticsTypeName}"))
		{
			if (packCount > 0)
			{
				for (int i = 0; i < packCount * PackWidth; i++) builder.Line($"ulong count{i}");

				builder.NewSection();
				Sum();

				builder.NewSection();
				SumAvx2();

				builder.NewSection();
				SumSoftware();
			}
			else Sum();
		}

		context.AddSource("Statistics.fields.g.cs", builder.ToString());

		void Sum()
		{
			using var _ = builder.FetchBlock($"public static unsafe partial {StatisticsTypeName} Sum({StatisticsTypeName}* source, int length)");

			if (packCount == 0)
			{
				builder.Line("return default");
				return;
			}

			builder.Line("if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length))");
			builder.Line("if (Avx2.IsSupported) return SumAvx2(source, length)");

			builder.NewSection();
			builder.Line("return SumSoftware(source, length)");
		}

		void SumAvx2()
		{
			builder.Attribute("SkipLocalsInit");
			using var _ = builder.FetchBlock($"static unsafe {StatisticsTypeName} SumAvx2({StatisticsTypeName}* source, int length)");

			builder.Line($"Unsafe.SkipInit(out {StatisticsTypeName} target)");
			builder.Line("ulong* ptrTarget = (ulong*)&target");

			builder.NewSection();
			using (builder.FetchBlock($"for (int i = 0; i < {packCount}; i++)"))
			{
				builder.Line($"int offset = i * {PackWidth}");
				builder.Line("ulong* ptrSource = (ulong*)source + offset");
				builder.Line("Vector256<ulong> accumulator = Avx.LoadVector256(ptrSource)");

				builder.NewSection();
				using (builder.FetchBlock("for (int j = 1; j < length; j++)"))
				{
					builder.Line($"accumulator = Avx2.Add(accumulator, Avx.LoadVector256(ptrSource + j * {ulongCount}))");
				}

				builder.NewSection();
				builder.Line("Avx.Store(ptrTarget + offset, accumulator)");
			}

			builder.NewSection();
			builder.Line("return target");
		}

		void SumSoftware()
		{
			using var _ = builder.FetchBlock($"static unsafe {StatisticsTypeName} SumSoftware({StatisticsTypeName}* source, int length)");

			builder.Line($"{StatisticsTypeName} target = *source");

			builder.NewSection();
			using (builder.FetchBlock("for (int i = 1; i < length; i++)"))
			{
				builder.Line($"ref readonly var refSource = ref Unsafe.AsRef<{StatisticsTypeName}>(source + i)");

				builder.NewSection();
				for (int i = 0; i < packCount * PackWidth; i++)
				{
					builder.Line($"target.count{i} += refSource.count{i}");
				}
			}

			builder.NewSection();
			builder.Line("return target");
		}
	}

	static void CreateMethods(SourceProductionContext context, string[] literals)
	{
		Array.Sort(literals, LengthComparer);

		context.CancellationToken.ThrowIfCancellationRequested();
		var builder = new SourceBuilder(nameof(StatisticsGenerator));

		builder.NewSection();
		builder.Using("System");
		builder.Using("System.Runtime.CompilerServices");

		builder.NewSection();
		builder.Namespace(NamespaceName);

		builder.NewSection();
		using (builder.FetchBlock($"partial struct {StatisticsTypeName}"))
		{
			Report();
		}

		context.AddSource("Statistics.methods.g.cs", builder.ToString());

		void Report()
		{
			builder.Attribute("MethodImpl", "MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization");
			using var _ = builder.FetchBlock($"public partial void {ReportMethodName}(string {LabelParameterName})");

			if (literals.Length == 0)
			{
				builder.Line($"throw new ArgumentOutOfRangeException({LabelParameterName})");
				return;
			}

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

		static int LengthComparer(string value0, string value1) => value0.Length.CompareTo(value1.Length);
	}
}