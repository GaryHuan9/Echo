using System.Linq;
using CodeHelpers.Mathematics;

namespace ForceRenderer.Rendering.Tiles
{
	public class OrderedPattern : ITilePattern
	{
		public virtual Int2[] GetPattern(Int2 totalSize) => totalSize.Loop().ToArray();
	}
}