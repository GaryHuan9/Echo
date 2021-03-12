using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;
using ForceRenderer.Rendering.Pixels;

namespace ForceRenderer.Rendering.Materials
{
	/// <summary>
	/// Represents a completely invisible material. The <see cref="PressedScene"/>
	/// should omit all geometry tagged with this material.
	/// </summary>
	public class Invisible : Material
	{
		public override Float3 Emit(in CalculatedHit hit, ExtendedRandom random) => Float3.zero;

		public override Float3 BidirectionalScatter(in CalculatedHit hit, ExtendedRandom random, out Float3 direction)
		{
			direction = hit.direction;
			return Float3.one;
		}
	}
}