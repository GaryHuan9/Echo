using System;
using Microsoft.CodeAnalysis;

namespace Echo.Generation;

partial class StatisticsGenerator
{
	static class Generation
	{
		/// <summary>
		/// Creates the source containing all members that only depend on the pack count.
		/// A pack is a group of <see cref="PackWidth"/> ulong to be calculated with AVX2.
		/// </summary>
		public static void CreateMembersWithoutLabels(SourceProductionContext context, int packCount)
		{
			var builder = new SourceBuilder(nameof(StatisticsGenerator));

			builder.NewLine("using System");
			builder.NewLine("using System.Runtime.CompilerServices");
			builder.NewLine("using System.Runtime.InteropServices");
			builder.NewLine("using System.Runtime.Intrinsics");
			builder.NewLine("using System.Runtime.Intrinsics.X86");

			builder.NewLine();
			builder.NewLine($"namespace {NamespaceName}");

			builder.NewLine();
			int ulongCount = CeilingDivide(packCount, LineWidth) * PackWidth * LineWidth;
			int structSize = Math.Max(ulongCount * sizeof(ulong), 1);
			builder.Attribute("StructLayout", $"LayoutKind.Sequential, Size = {structSize}");
			using (builder.FetchBlock($"partial struct {StatisticsTypeName}"))
			{
				if (packCount > 0)
				{
					FieldCounts();

					builder.NewLine();
					MethodSum();

					builder.NewLine();
					MethodSumAvx2();

					builder.NewLine();
					MethodSumSoftware();
				}
				else MethodSum();
			}

			context.AddSource("Statistics.fields.g.cs", builder.ToString());

			void FieldCounts()
			{
				for (int i = 0; i < packCount * PackWidth; i++) builder.NewLine($"ulong count{i}");
			}

			void MethodSum()
			{
				using var _ = builder.FetchBlock($"public static unsafe partial {StatisticsTypeName} Sum({StatisticsTypeName}* source, int length)");

				if (packCount == 0)
				{
					builder.NewLine("return default");
					return;
				}

				builder.NewLine("if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length))");
				builder.NewLine("if (Avx2.IsSupported) return SumAvx2(source, length)");

				builder.NewLine();
				builder.NewLine("return SumSoftware(source, length)");
			}

			void MethodSumAvx2()
			{
				builder.Attribute("SkipLocalsInit");
				using var _ = builder.FetchBlock($"static unsafe {StatisticsTypeName} SumAvx2({StatisticsTypeName}* source, int length)");

				builder.NewLine($"Unsafe.SkipInit(out {StatisticsTypeName} target)");
				builder.NewLine("ulong* ptrTarget = (ulong*)&target");

				builder.NewLine();
				using (builder.FetchBlock($"for (int i = 0; i < {packCount}; i++)"))
				{
					builder.NewLine($"int offset = i * {PackWidth}");
					builder.NewLine("ulong* ptrSource = (ulong*)source + offset");
					builder.NewLine("Vector256<ulong> accumulator = Avx.LoadVector256(ptrSource)");

					builder.NewLine();
					using (builder.FetchBlock("for (int j = 1; j < length; j++)"))
					{
						builder.NewLine($"accumulator = Avx2.Add(accumulator, Avx.LoadVector256(ptrSource + j * {ulongCount}))");
					}

					builder.NewLine();
					builder.NewLine("Avx.Store(ptrTarget + offset, accumulator)");
				}

				builder.NewLine();
				builder.NewLine("return target");
			}

			void MethodSumSoftware()
			{
				using var _ = builder.FetchBlock($"static unsafe {StatisticsTypeName} SumSoftware({StatisticsTypeName}* source, int length)");

				builder.NewLine($"{StatisticsTypeName} target = *source");

				builder.NewLine();
				using (builder.FetchBlock("for (int i = 1; i < length; i++)"))
				{
					builder.NewLine($"ref readonly var refSource = ref Unsafe.AsRef<{StatisticsTypeName}>(source + i)");

					builder.NewLine();
					for (int i = 0; i < packCount * PackWidth; i++)
					{
						builder.NewLine($"target.count{i} += refSource.count{i}");
					}
				}

				builder.NewLine();
				builder.NewLine("return target");
			}
		}

		/// <summary>
		/// Creates the source containing all members that depend on the actual string labels/literals.
		/// </summary>
		public static void CreateMembersWithLabels(SourceProductionContext context, string[] labels)
		{
			var builder = new SourceBuilder(nameof(StatisticsGenerator));

			builder.NewLine("using System");
			builder.NewLine("using System.Runtime.CompilerServices");

			builder.NewLine();
			builder.NewLine($"namespace {NamespaceName}");

			Array.Sort(labels, StringComparer.OrdinalIgnoreCase);

			builder.NewLine();
			using (builder.FetchBlock($"partial struct {StatisticsTypeName}"))
			{
				FieldCountLabels();

				builder.NewLine();
				MethodReport();

				builder.NewLine();
				MethodIndexerImpl();

				builder.NewLine();
				MethodCountImpl();
			}

			context.AddSource("Statistics.methods.g.cs", builder.ToString());

			void FieldCountLabels()
			{
				if (labels.Length == 0) return;

				using (builder.FetchBlock("static readonly string[] countLabels = "))
				{
					const int LineWrapThreshold = 50;

					builder.Indent();
					int lineLength = 0;

					foreach (string label in labels)
					{
						if (lineLength > LineWrapThreshold)
						{
							builder.NewLine();
							builder.Indent();
							lineLength = 0;
						}

						builder.Append($"\"{label}\", ");
						lineLength += label.Length;
					}

					builder.NewLine();
				}

				//Add the missing semicolon at the end of the array declaration
				//The generated syntax is a bit weird but I am lazy and this works

				builder.NewLine("");
			}

			void MethodReport()
			{
				builder.Attribute("MethodImpl", "MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization");
				using var _ = builder.FetchBlock($"public partial void {MethodName}(string label)");

				if (labels.Length == 0)
				{
					builder.NewLine("throw new ArgumentOutOfRangeException(label)");
					return;
				}

				//Create an array of indices of the labels when they are sorted alphabetically,
				//then sort the indices based on the length of the labels, so we can categorize
				//them into different switch cases.

				int[] labelIndices = new int[labels.Length];
				for (int i = 0; i < labels.Length; i++) labelIndices[i] = i;

				//Span.Sort does not exist in .NETStandard 2.0 :(
				Array.Sort(labelIndices, LabelLengthComparer);

				using (builder.FetchBlock("switch (label.Length)"))
				{
					for (int index = 0; index < labels.Length;)
					{
						string label = labels[labelIndices[index]];
						int length = label.Length;
						int startIndex = index;

						using (builder.FetchBlock($"case {length}:"))
						{
							do
							{
								if (index > startIndex)
								{
									label = labels[labelIndices[index]];
									if (label.Length != length) break;
									builder.Prefix("else if (");
								}
								else builder.Prefix("if (");

								for (int i = 0; i < length; i++)
								{
									builder.Append($"label[{i}] == '{label[i]}'");
									if (i + 1 < length) builder.Append(" && ");
								}

								builder.Postfix($") ++count{labelIndices[index]}");
							}
							while (++index < labels.Length);

							builder.NewLine("else break");
							builder.NewLine("return");
						}
					}
				}

				builder.NewLine();
				builder.NewLine("throw new ArgumentOutOfRangeException(label)");

				int LabelLengthComparer(int index0, int index1) => labels[index0].Length.CompareTo(labels[index1].Length);
			}

			void MethodIndexerImpl()
			{
				using var _ = builder.FetchBlock("private partial (string label, ulong count) IndexerImpl(int index)");

				if (labels.Length > 0)
				{
					builder.NewLine($"if ((uint)index >= {labels.Length}) throw new ArgumentOutOfRangeException(nameof(index))");
					builder.NewLine("return (label: countLabels[index], count: Unsafe.Add<ulong>(ref count0, index))");
				}
				else builder.NewLine("throw new ArgumentOutOfRangeException(nameof(index))");
			}

			void MethodCountImpl()
			{
				using var _ = builder.FetchBlock("private static partial int CountImpl()");
				builder.NewLine($"return {labels.Length}");
			}
		}
	}
}