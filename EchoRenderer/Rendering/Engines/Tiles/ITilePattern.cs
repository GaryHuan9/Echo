using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.Engines.Tiles
{
	public interface ITilePattern
	{
		/// <summary>
		/// Returns a sequence of tile positions from (0, 0) (Inclusive) to <paramref name="totalSize"/> (Exclusive) for rendering.
		/// NOTE: The returned positions are not scaled by tile size, meaning each positions should be just consecutive to its neighbor.
		/// </summary>
		Int2[] GetPattern(Int2 totalSize);
	}
}