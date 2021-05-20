using CodeHelpers.Collections;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.Engines.Tiles
{
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