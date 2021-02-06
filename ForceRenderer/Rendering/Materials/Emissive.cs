using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;

namespace ForceRenderer.Rendering.Materials
{
	public class Emissive : Diffuse
	{
		public Emissive() => Albedo = Float3.zero;

		public Float3 Emission { get; set; }

		public override void Press()
		{
			base.Press();
			AssertNonNegative(Albedo);
		}

		public override Float3 Emit(in CalculatedHit hit, ExtendedRandom random) => Emission;
	}
}