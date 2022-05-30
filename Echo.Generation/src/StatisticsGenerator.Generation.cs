using System;
using Microsoft.CodeAnalysis;

namespace Echo.Generation;

partial class StatisticsGenerator
{
	const int PackWidth = 4; //How many UInt64s are packed together
	const int LineWidth = 2; //How many packs per 64-byte cache line

	static class Generation
	{
		/// <summary>
		/// Creates the source containing all members that only depend on the pack count.
		/// A pack is a group of <see cref="PackWidth"/> ulong to be calculated with AVX2.
		/// </summary>
		public static void CreateMembersWithoutLabels(SourceProductionContext context, TypePair<int> pair)
		{
			(FlatType type, int packCount) = pair;

			var builder = new SourceBuilder(nameof(StatisticsGenerator));

			builder.NewCode("using System");
			builder.NewCode("using System.Runtime.CompilerServices");
			builder.NewCode("using System.Runtime.InteropServices");
			builder.NewCode("using System.Runtime.Intrinsics");
			builder.NewCode("using System.Runtime.Intrinsics.X86");

			builder.NewLine();
			builder.NewCode($"namespace {type.Namespace}");

			builder.NewLine();
			int lineCount = CeilingDivide(packCount, LineWidth);
			int ulongCount = lineCount * PackWidth * LineWidth;

			int structSize = Math.Max(lineCount, 1) * PackWidth * LineWidth * sizeof(ulong);
			builder.Attribute("StructLayout", $"LayoutKind.Sequential, Size = {structSize}");
			using (builder.FetchBlock($"partial struct {type.TypeName}"))
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

			context.AddSource($"{type.TypeName}.fields.g.cs", builder.ToString());

			void FieldCounts()
			{
				for (int i = 0; i < packCount * PackWidth; i++) builder.NewCode($"ulong count{i}");
			}

			void MethodSum()
			{
				builder.NewLine("/// <inheritdoc/>");
				using var _ = builder.FetchBlock($"public unsafe {type.TypeName} Sum({type.TypeName}* source, int length)");

				builder.NewCode("if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length))");

				if (packCount == 0)
				{
					builder.NewCode("return default");
					return;
				}

				builder.NewCode("if (Avx2.IsSupported) return SumAvx2(source, length)");
				builder.NewCode("return SumSoftware(source, length)");
			}

			void MethodSumAvx2()
			{
				builder.Attribute("SkipLocalsInit");
				using var _ = builder.FetchBlock($"static unsafe {type.TypeName} SumAvx2({type.TypeName}* source, int length)");

				builder.NewCode($"Unsafe.SkipInit(out {type.TypeName} target)");
				builder.NewCode("ulong* ptrTarget = (ulong*)&target");

				builder.NewLine();
				using (builder.FetchBlock($"for (int i = 0; i < {packCount}; i++)"))
				{
					builder.NewCode($"int offset = i * {PackWidth}");
					builder.NewCode("ulong* ptrSource = (ulong*)source + offset");
					builder.NewCode("Vector256<ulong> accumulator = Avx.LoadVector256(ptrSource)");

					builder.NewLine();
					using (builder.FetchBlock("for (int j = 1; j < length; j++)"))
					{
						builder.NewCode($"accumulator = Avx2.Add(accumulator, Avx.LoadVector256(ptrSource + j * {ulongCount}))");
					}

					builder.NewLine();
					builder.NewCode("Avx.Store(ptrTarget + offset, accumulator)");
				}

				builder.NewLine();
				builder.NewCode("return target");
			}

			void MethodSumSoftware()
			{
				using var _ = builder.FetchBlock($"static unsafe {type.TypeName} SumSoftware({type.TypeName}* source, int length)");

				builder.NewCode($"{type.TypeName} target = *source");

				builder.NewLine();
				using (builder.FetchBlock("for (int i = 1; i < length; i++)"))
				{
					builder.NewCode($"ref readonly var refSource = ref Unsafe.AsRef<{type.TypeName}>(source + i)");

					builder.NewLine();
					for (int i = 0; i < packCount * PackWidth; i++)
					{
						builder.NewCode($"target.count{i} += refSource.count{i}");
					}
				}

				builder.NewLine();
				builder.NewCode("return target");
			}
		}

		/// <summary>
		/// Creates the source containing all members that depend on the actual string labels/literals.
		/// </summary>
		public static void CreateMembersWithLabels(SourceProductionContext context, TypePair<StringSet> pair)
		{
			(FlatType type, StringSet set) = pair;

			string[] labels = set.ToArray();
			Array.Sort(labels, StringComparer.OrdinalIgnoreCase);
			context.CancellationToken.ThrowIfCancellationRequested();

			var builder = new SourceBuilder(nameof(StatisticsGenerator));

			builder.NewCode("using System");
			builder.NewCode("using System.Runtime.CompilerServices");
			builder.NewCode($"using {NamespaceName}");

			builder.NewLine();
			builder.NewCode($"namespace {type.Namespace}");

			builder.NewLine();
			using (builder.FetchBlock($"partial struct {type.TypeName}"))
			{
				FieldEventLabels();

				builder.NewLine();
				PropertyCount();

				builder.NewLine();
				PropertyIndexer();

				builder.NewLine();
				MethodReport();
			}

			context.AddSource($"{type.TypeName}.methods.g.cs", builder.ToString());

			void FieldEventLabels()
			{
				if (labels.Length == 0) return;

				using (builder.FetchBlock("static readonly string[] eventLabels = "))
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

				builder.NewCode("");
			}

			void PropertyCount()
			{
				builder.NewLine("/// <inheritdoc/>");
				builder.NewCode($"public int Count => {labels.Length}");
			}

			void PropertyIndexer()
			{
				builder.NewLine("/// <inheritdoc/>");
				using var _0 = builder.FetchBlock("public EventRow this[int index]");
				using var _1 = builder.FetchBlock("get");

				if (labels.Length > 0)
				{
					builder.NewCode($"if ((uint)index >= {labels.Length}) throw new ArgumentOutOfRangeException(nameof(index))");
					builder.NewCode("return new EventRow(eventLabels[index], Unsafe.Add<ulong>(ref count0, index))");
				}
				else builder.NewCode("throw new ArgumentOutOfRangeException(nameof(index))");
			}

			void MethodReport()
			{
				builder.NewLine("/// <inheritdoc/>");
				builder.Attribute("MethodImpl", "MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization");
				using var _ = builder.FetchBlock($"public void {MethodName}(string label, ulong count = 1)");

				if (labels.Length == 0)
				{
					builder.NewCode("throw new ArgumentOutOfRangeException(nameof(label))");
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

								builder.Postfix($") count{labelIndices[index]} += count");
							}
							while (++index < labels.Length);

							builder.NewCode("else break");
							builder.NewCode("return");
						}
					}
				}

				builder.NewLine();
				builder.NewCode("throw new ArgumentOutOfRangeException(nameof(label))");

				int LabelLengthComparer(int index0, int index1) => labels[index0].Length.CompareTo(labels[index1].Length);
			}
		}
	}
}