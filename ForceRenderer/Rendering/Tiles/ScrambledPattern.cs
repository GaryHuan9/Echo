using CodeHelpers.Collections;
using CodeHelpers.Mathematics;

namespace ForceRenderer.Rendering.Tiles
{
	public class ScrambledPattern : OrderedPattern
	{
		public override Int2[] GetPattern(Int2 totalSize)
		{
			Int2[] array = base.GetPattern(totalSize);

			array.Shuffle();
			return array;
		}
	}
}