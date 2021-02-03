using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
using ForceRenderer.Renderers;

namespace ForceRenderer.IO
{
	public abstract class MaterialNew
	{
		/// <summary>
		/// Returns the emission of this material.
		/// </summary>
		public abstract Float3 Emit(in CalculatedHit hit, ExtendedRandom random);

		/// <summary>
		/// Returns the bidirectional scattering distribution function value of
		/// this material and outputs the randomly scattered direction.
		/// </summary>
		public abstract Float3 BidirectionalScatter(in CalculatedHit hit, ExtendedRandom random, out Float3 direction);
	}
}