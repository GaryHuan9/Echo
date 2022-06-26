using System;
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

public class HilbertCurvePattern : ITilePattern
{
	public Int2[] CreateSequence(Int2 size)
	{
		Int2 topRightSize, topLeftSize, bottomRightSize, bottomLeftSize;
		topRightSize = new Int2((int)Math.Ceiling(size.X / 2.0), (int)Math.Floor(size.Y / 2.0));
		topLeftSize = new Int2((int)Math.Floor(size.X / 2.0), (int)Math.Floor(size.Y / 2.0));
		bottomRightSize = new Int2((int)Math.Ceiling(size.X / 2.0), (int)Math.Ceiling(size.Y / 2.0));
		bottomLeftSize = new Int2((int)Math.Floor(size.X / 2.0), (int)Math.Ceiling(size.Y / 2.0));

		var topRightCorner = topRightSize.X > topRightSize.Y ?
			Hilbert2D(Int2.Zero, new Int2(topRightSize.X, 0), new Int2(0, topRightSize.Y)).ToArray() :
			Hilbert2D(Int2.Zero, new Int2(0, topRightSize.Y), new Int2(topRightSize.X, 0)).ToArray();

		var topLeftCorner = topLeftSize.X > topLeftSize.Y ?
			Hilbert2D(Int2.Zero, new Int2(topLeftSize.X, 0), new Int2(0, topLeftSize.Y)).ToArray() :
			Hilbert2D(Int2.Zero, new Int2(0, topLeftSize.Y), new Int2(topLeftSize.X, 0)).ToArray();

		var bottomRightCorner = bottomRightSize.X > bottomRightSize.Y ?
			Hilbert2D(Int2.Zero, new Int2(bottomRightSize.X, 0), new Int2(0, bottomRightSize.Y)).ToArray() :
			Hilbert2D(Int2.Zero, new Int2(0, bottomRightSize.Y), new Int2(bottomRightSize.X, 0)).ToArray();

		var bottomLeftCorner = bottomLeftSize.X > bottomLeftSize.Y ?
			Hilbert2D(Int2.Zero, new Int2(bottomLeftSize.X, 0), new Int2(0, bottomLeftSize.Y)).ToArray() :
			Hilbert2D(Int2.Zero, new Int2(0, bottomLeftSize.Y), new Int2(bottomLeftSize.X, 0)).ToArray();

		// offset and interlace all corners
		Int2[] result = new Int2[topLeftCorner.Length + topRightCorner.Length + bottomLeftCorner.Length + bottomRightCorner.Length];

		for (int i = 0; i < topLeftCorner.Length; i++)
		{
			topLeftCorner[i] = new Int2(topLeftSize.X - topLeftCorner[i].X - 1, topLeftSize.Y - topLeftCorner[i].Y - 1);
		}

		for (int i = 0; i < topRightCorner.Length; i++)
		{
			topRightCorner[i] += new Int2(topLeftSize.X, 0);
			topRightCorner[i] = new Int2(topRightCorner[i].X, topRightSize.Y - topRightCorner[i].Y - 1);
		}

		for (int i = 0; i < bottomLeftCorner.Length; i++)
		{
			bottomLeftCorner[i] += new Int2(0, topLeftSize.Y);
			bottomLeftCorner[i] = new Int2(bottomLeftSize.X - bottomLeftCorner[i].X - 1, bottomLeftCorner[i].Y);
		}

		for (int i = 0; i < bottomRightCorner.Length; i++)
		{
			bottomRightCorner[i] += topLeftSize;
		}

		int topLeft = 0, topRight = 0, bottomLeft = 0, bottomRight = 0;

		for (int i = 0; i < result.Length;)
		{
			if (topLeft < topLeftCorner.Length) result[i++] = topLeftCorner[topLeft++];
			if (topRight < topRightCorner.Length) result[i++] = topRightCorner[topRight++];
			if (bottomLeft < bottomLeftCorner.Length) result[i++] = bottomLeftCorner[bottomLeft++];
			if (bottomRight < bottomRightCorner.Length) result[i++] = bottomRightCorner[bottomRight++];
		}


		return result;
	}

	IEnumerable<Int2> Hilbert2D(Int2 position, Int2 rectA, Int2 rectB)
	{
		Int2 size = new Int2(rectA.Sum, rectB.Sum).Absoluted;

		Int2 da = rectA.Signed; // unit major direction
		Int2 db = rectB.Signed; // unit orthogonal direction

		if (size.X < 1 || size.Y < 1) throw new Exception("size is less than 1!");

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
				if (size2.Y % 2 != 0 && size.Y > 2)
				{
					rectB2 += db;
				}

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