using System.Linq;
using CodeHelpers.Mathematics;
using CodeHelpers.Mathematics.Enumerables;

namespace ForceRenderer.Rendering.Tiles
{
	public class SpiralPattern : ITilePattern
	{
		public virtual Int2[] GetPattern(Int2 totalSize) => (from position in new EnumerableSpiral2D(totalSize.MaxComponent.CeiledDivide(2))
															 let tile = position + totalSize / 2 - Int2.one
															 where tile.x >= 0 && tile.y >= 0 && tile.x < totalSize.x && tile.y < totalSize.y
															 select tile).ToArray();
	}
}