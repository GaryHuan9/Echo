using System.Linq;
using CodeHelpers.Collections;
using CodeHelpers.Mathematics;
using CodeHelpers.Mathematics.Enumerable;

namespace EchoRenderer.Rendering.Engines
{
	public interface ITilePattern
	{
		/// <summary>
		/// Returns a sequence of tile positions from (0, 0) (Inclusive) to <paramref name="totalSize"/> (Exclusive) for rendering.
		/// NOTE: The returned positions are not scaled by tile size, meaning each positions should be consecutive to its neighbors.
		/// </summary>
		Int2[] GetPattern(Int2 totalSize);
	}

	public class OrderedPattern : ITilePattern
	{
		/// <inheritdoc/>
		public virtual Int2[] GetPattern(Int2 totalSize) => totalSize.Loop().ToArray();
	}

	public class ScrambledPattern : OrderedPattern
	{
		public override Int2[] GetPattern(Int2 totalSize)
		{
			Int2[] array = base.GetPattern(totalSize);

			array.Shuffle();
			return array;
		}
	}

	public class SpiralPattern : ITilePattern
	{
		/// <inheritdoc/>
		public virtual Int2[] GetPattern(Int2 totalSize) => (from position in new EnumerableSpiral2D(totalSize.MaxComponent.CeiledDivide(2))
															 let tile = position + totalSize / 2 - Int2.one
															 where Int2.zero <= tile && tile < totalSize
															 select tile).ToArray();
	}

	public class CheckerboardPattern : SpiralPattern
	{
		public override Int2[] GetPattern(Int2 totalSize)
		{
			Int2[] array = base.GetPattern(totalSize);
			for (int i = 0; i < array.Length / 2; i += 2) array.Swap(i, array.Length - i - 1);

			return array;
		}
	}
}