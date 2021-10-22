using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Scattering;

namespace EchoRenderer.Rendering.Materials
{
	public abstract class MaterialNew
	{
		/// <summary>
		/// Determines the scattering properties of this material at <paramref name="query"/>.
		/// Initializes a <see cref="BidirectionalScatteringDistributionFunctions"/>.
		/// </summary>
		public abstract void Scatter(ref HitQuery query, Arena arena, TransportMode mode);
	}
}