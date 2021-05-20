using System.Linq;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.Engines.Tiles
{
	public class OrderedPattern : ITilePattern
	{
		public virtual Int2[] GetPattern(Int2 totalSize) => totalSize.Loop().ToArray();
	}
}