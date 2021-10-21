using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Rendering.Scattering;

namespace EchoRenderer.Rendering.Materials
{
	public interface IMaterial
	{
		/// <summary>
		/// Determines the scattering properties of this material at <paramref name="query"/>.
		/// Initializes a <see cref="BidirectionalScatteringDistributionFunctions"/>.
		/// </summary>
		void Scatter(ref HitQuery query, MemoryArena arena, TransportMode mode);
	}
}