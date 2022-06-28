using System;
using System.Collections.Generic;
using System.Linq;
using CodeHelpers.Packed;

namespace Echo.Core.Evaluation.Operations;

public class HilbertCurvePattern : ITilePattern
{
	public Int2[] CreateSequence(Int2 size)
	{
		var result = size.X > size.Y ?
			Hilbert2D(Int2.Zero, new Int2(size.X, 0), new Int2(0, size.Y)).ToArray() :
			Hilbert2D(Int2.Zero, new Int2(0, size.Y), new Int2(size.X, 0)).ToArray();

		// flip first half of the array to make it start from the middle
		int halfLength = result.Length / 2;
		Int2[] tempArr = new Int2[halfLength];
		for (int i = 0; i < halfLength; i++)
			tempArr[i] = result[halfLength - i - 1];

		for (int i = 0; i < halfLength; i++)
			result[i] = tempArr[i];

		for (int i = 1; i < halfLength; i+=2)
		{
			Int2 temp = result[i + halfLength];
			result[i + halfLength] = result[i];
			result[i] = temp;
		}

		return result;
	}

	IEnumerable<Int2> Hilbert2D(Int2 position, Int2 rectA, Int2 rectB)
	{
		Int2 size = new Int2(Math.Abs(rectA.X + rectA.Y), Math.Abs(rectB.X + rectB.Y));

		Int2 da = new Int2(Math.Sign(rectA.X), Math.Sign(rectA.Y)); // unit major direction
		Int2 db = new Int2(Math.Sign(rectB.X), Math.Sign(rectB.Y)); // unit orthogonal direction

		if (size.X < 1 || size.Y < 1) throw new Exception("size is less than 1!");

		if (size.Y == 1)
		{
			// trivial row fill
			for (int i = 0; i < size.X; i++)
			{
				//yield return new Int2(position.X + da.X, position.Y + da.Y);
				yield return position;
				position += da;
			}
		}
		else if (size.X == 1)
		{
			// trivial column fill
			for (int i = 0; i < size.Y; i++)
			{
				//yield return new Int2(position.X + db.X, position.Y + db.Y);
				yield return position;
				position += db;
			}
		}
		else
		{
			Int2 rectA2 = rectA.FlooredDivide(2);
			Int2 rectB2 = rectB.FlooredDivide(2);
			Int2 size2 = new Int2(Math.Abs(rectA2.X + rectA2.Y), Math.Abs(rectB2.X + rectB2.Y));

			if (size.X * 2 > size.Y * 3)
			{
				if (size2.X % 2 != 0 && size.X > 2)
				{
					rectA2 += da;
				}

				var result = Hilbert2D(position, rectA2, rectB);
				foreach (var res in result) yield return res;

				result = Hilbert2D(position + rectA2, rectA - rectA2, rectB);
				foreach (var res in result) yield return res;
			}
			else
			{
				if (size2.Y % 2 != 0 && size.Y > 2)
				{
					rectB2 += db;
				}

				var result = Hilbert2D(position, rectB2, rectA2);
				foreach (var res in result) yield return res;

				result = Hilbert2D(position + rectB2, rectA, rectB - rectB2);
				foreach (var res in result) yield return res;

				result = Hilbert2D(position + (rectA - da) + (rectB2 - db), -rectB2, -(rectA - rectA2));
				foreach (var res in result) yield return res;
			}
		}
	}
}