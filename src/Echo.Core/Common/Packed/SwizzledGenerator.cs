using System;
using System.IO;

namespace Echo.Core.Common.Packed;

/// <summary>
/// "Code generate" for <see cref="Float4"/> swizzles. Only generates Float4 -> Float4 for now.
/// </summary>
public static class SwizzledGenerator
{
	const string Elements = "XYZW_";
	const int EmptyIndex = 4;

	public static void Execute(string path)
	{
		using var stream = File.Open(path, FileMode.Create);
		using var writer = new StreamWriter(stream);
		ExecuteCore(writer);
	}

	static void ExecuteCore(TextWriter writer)
	{
		Span<char> name = stackalloc char[4];
		Span<int> indices = stackalloc int[4];
		Span<int> copy = stackalloc int[4];

		for (indices[0] = 0; indices[0] < 5; ++indices[0])
		for (indices[1] = 0; indices[1] < 5; ++indices[1])
		for (indices[2] = 0; indices[2] < 5; ++indices[2])
		{
			for (indices[3] = 0; indices[3] < 5; ++indices[3])
			{
				name[0] = Elements[indices[0]];
				name[1] = Elements[indices[1]];
				name[2] = Elements[indices[2]];
				name[3] = Elements[indices[3]];

				indices.CopyTo(copy);
				ScanIndices(copy, out bool hasEmpty, out bool isOrdered);

				writer.Write("		[EB(EBS.Never), DB(DBS.Never)] public F4 ");
				writer.Write(name);
				writer.Write(" => ");

				if (hasEmpty)
				{
					//This is not the most optimal code gen for when there are empties involved, but it is pretty good most of the times.

					if (isOrdered)
					{
						writer.Write("Blend(");
						WriteBlendMask(writer, indices);
						writer.Write(')');
					}
					else
					{
						writer.Write("ShuffleBlend(");
						WriteShuffleMask(writer, copy);
						writer.Write(", ");
						WriteBlendMask(writer, indices);
						writer.Write(')');
					}
				}
				else
				{
					writer.Write("Shuffle(");
					WriteShuffleMask(writer, indices);
					writer.Write(')');
				}

				writer.WriteLine(';');
			}

			writer.WriteLine();
		}
	}

	static void ScanIndices(Span<int> indices, out bool hasEmpty, out bool isOrdered)
	{
		hasEmpty = false;
		isOrdered = true;

		for (int i = 0; i < 4; i++)
		{
			ref int index = ref indices[i];

			if (index == EmptyIndex)
			{
				hasEmpty = true;
				index = i;
			}

			if (index != i) isOrdered = false;
		}
	}

	static void WriteShuffleMask(TextWriter writer, Span<int> indices)
	{
		//(x << 0) | (y << 2) | (z << 4) | (w << 6)

		for (int i = 0; i < 4; i++)
		{
			writer.Write('(');
			writer.Write(indices[i]);
			writer.Write(" << ");
			writer.Write(i * 2);
			writer.Write(')');

			if (i < 3) writer.Write(" | ");
		}
	}

	static void WriteBlendMask(TextWriter writer, Span<int> indices)
	{
		//0bwzyx

		writer.Write("0b");
		for (int i = 3; i >= 0; i--) writer.Write(indices[i] == EmptyIndex ? '1' : '0');
	}
}