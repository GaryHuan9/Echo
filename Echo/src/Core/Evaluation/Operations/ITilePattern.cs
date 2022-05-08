using System.Collections.Generic;
using System.Linq;
using CodeHelpers.Collections;
using CodeHelpers.Mathematics;
using CodeHelpers.Mathematics.Enumerable;
using CodeHelpers.Packed;

namespace Echo.Core.Evaluation.Operations;

public interface ITilePattern
{
	/// <summary>
	/// Returns a series of tile positions from (0, 0) (inclusive) to <paramref name="size"/> (exclusive).
	/// </summary>
	Int2[] CreateSequence(Int2 size);
}

public class OrderedPattern : ITilePattern
{
	/// <inheritdoc/>
	public virtual Int2[] CreateSequence(Int2 size) => size.Loop().ToArray();
}

public class ScrambledPattern : OrderedPattern
{
	public override Int2[] CreateSequence(Int2 size)
	{
		Int2[] array = base.CreateSequence(size);

		array.Shuffle();
		return array;
	}
}

public class SpiralPattern : ITilePattern
{
	/// <inheritdoc/>
	public virtual Int2[] CreateSequence(Int2 size)
	{
		int width = size.MaxComponent.CeiledDivide(2);

		return (from position in new EnumerableSpiral2D(width)
				let tile = position + size / 2 - Int2.One
				where Int2.Zero <= tile && tile < size
				select tile).ToArray();
	}
}

public class CheckerboardPattern : SpiralPattern
{
	public override Int2[] CreateSequence(Int2 size)
	{
		Int2[] array = base.CreateSequence(size);
		for (int i = 0; i < array.Length / 2; i += 2) array.Swap(i, array.Length - i - 1);

		return array;
	}
}