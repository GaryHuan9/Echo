using System.Collections.Generic;
using System.Linq;
using Echo.Core.Common;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Enumeration;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Common.Packed;

namespace Echo.Core.Evaluation.Operation;

public interface ITilePattern
{
	/// <summary>
	/// Returns a series of tile positions from (0, 0) (inclusive) to <paramref name="size"/> (exclusive).
	/// </summary>
	public Int2[] CreateSequence(Int2 size);
}

public class OrderedPattern : ITilePattern
{
	public OrderedPattern(bool horizontal = true) => this.horizontal = horizontal;

	readonly bool horizontal;

	/// <inheritdoc/>
	public virtual Int2[] CreateSequence(Int2 size) => (horizontal ?
		size.Loop() :
		size.YX.Loop().Select(position => position.YX)).ToArray();
}

public class ScrambledPattern : OrderedPattern
{
	public override Int2[] CreateSequence(Int2 size)
	{
		Int2[] array = base.CreateSequence(size);
		SystemPrng.Shared.Shuffle<Int2>(array);
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
		for (int i = 0; i < array.Length / 2; i += 2) Utility.Swap(ref array[i], ref array[array.Length - i - 1]);

		return array;
	}
}

public class HilbertCurvePattern : ITilePattern
{
	public Int2[] CreateSequence(Int2 size)
	{
		if (size == Int2.One) return new[] { Int2.Zero };

		//Divide and get the hilbert curve for all four corners
		Int2 topRightSize = new Int2(size.X.CeiledDivide(2), size.Y.FlooredDivide(2));
		Int2 topLeftSize = new Int2(size.X.FlooredDivide(2), size.Y.FlooredDivide(2));
		Int2 bottomRightSize = new Int2(size.X.CeiledDivide(2), size.Y.CeiledDivide(2));
		Int2 bottomLeftSize = new Int2(size.X.FlooredDivide(2), size.Y.CeiledDivide(2));

		var topRightCorner = Hilbert2D(topRightSize);
		var topLeftCorner = Hilbert2D(topLeftSize);
		var bottomRightCorner = Hilbert2D(bottomRightSize);
		var bottomLeftCorner = Hilbert2D(bottomLeftSize);

		//Offset and interlace all corners
		for (int i = 0; i < topLeftCorner.Length; i++)
		{
			topLeftCorner[i] = topLeftSize - topLeftCorner[i] - Int2.One;
		}

		for (int i = 0; i < topRightCorner.Length; i++)
		{
			ref Int2 position = ref topRightCorner[i];

			position = new Int2
			(
				position.X + topLeftSize.X,
				topRightSize.Y - position.Y - 1
			);
		}

		for (int i = 0; i < bottomLeftCorner.Length; i++)
		{
			ref Int2 position = ref bottomLeftCorner[i];

			position = new Int2
			(
				bottomLeftSize.X - position.X - 1,
				position.Y + topLeftSize.Y
			);
		}

		for (int i = 0; i < bottomRightCorner.Length; i++)
		{
			bottomRightCorner[i] += topLeftSize;
		}

		int topLeft = 0, topRight = 0, bottomLeft = 0, bottomRight = 0;

		//Aggregate all corners into the result
		Int2[] result = new Int2[size.Product];

		for (int i = 0; i < result.Length;)
		{
			if (topLeft < topLeftCorner.Length) result[i++] = topLeftCorner[topLeft++];
			if (topRight < topRightCorner.Length) result[i++] = topRightCorner[topRight++];
			if (bottomLeft < bottomLeftCorner.Length) result[i++] = bottomLeftCorner[bottomLeft++];
			if (bottomRight < bottomRightCorner.Length) result[i++] = bottomRightCorner[bottomRight++];
		}

		return result;
	}

	static Int2[] Hilbert2D(Int2 size) => size.X > size.Y ?
		Hilbert2D(Int2.Zero, new Int2(size.X, 0), new Int2(0, size.Y)).ToArray() :
		Hilbert2D(Int2.Zero, new Int2(0, size.Y), new Int2(size.X, 0)).ToArray();

	static IEnumerable<Int2> Hilbert2D(Int2 position, Int2 rectA, Int2 rectB)
	{
		Int2 size = new Int2(rectA.Sum, rectB.Sum).Absoluted;

		Int2 da = rectA.Signed; // unit major direction
		Int2 db = rectB.Signed; // unit orthogonal direction

		if (size.Y == 1)
		{
			// trivial row fill
			for (int i = 0; i < size.X; i++)
			{
				yield return position;
				position += da;
			}
		}
		else if (size.X == 1)
		{
			// trivial column fill
			for (int i = 0; i < size.Y; i++)
			{
				yield return position;
				position += db;
			}
		}
		else
		{
			Int2 rectA2 = rectA / 2;
			Int2 rectB2 = rectB / 2;

			Int2 size2 = new Int2(rectA2.Sum, rectB2.Sum).Absoluted;

			if (size.X * 2 > size.Y * 3)
			{
				if (size2.X % 2 != 0 && size.X > 2)
				{
					rectA2 += da;
				}

				var result = Hilbert2D(position, rectA2, rectB);
				foreach (var item in result) yield return item;

				result = Hilbert2D(position + rectA2, rectA - rectA2, rectB);
				foreach (var item in result) yield return item;
			}
			else
			{
				if (size2.Y % 2 != 0 && size.Y > 2) rectB2 += db;

				var result = Hilbert2D(position, rectB2, rectA2);
				foreach (var item in result) yield return item;

				result = Hilbert2D(position + rectB2, rectA, rectB - rectB2);
				foreach (var item in result) yield return item;

				result = Hilbert2D(position + (rectA - da) + (rectB2 - db), -rectB2, -(rectA - rectA2));
				foreach (var item in result) yield return item;
			}
		}
	}
}