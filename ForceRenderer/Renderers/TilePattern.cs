using System.Linq;
using CodeHelpers.Collections;
using CodeHelpers.Vectors;

namespace ForceRenderer.Renderers
{
	public class TilePattern
	{
		public TilePattern(Int2 bufferSize, int tileSize)
		{
			Int2.LoopEnumerable grid = bufferSize.CeiledDivide(tileSize).Loop();
			pattern = grid.Select(position => position * tileSize).ToArray();

			//Shuffle it just for fun
			pattern.Shuffle();
		}

		readonly Int2[] pattern;
		public int Length => pattern.Length;

		public Int2 this[int index] => pattern[index];
	}
}