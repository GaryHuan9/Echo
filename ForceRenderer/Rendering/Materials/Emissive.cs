using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;

namespace ForceRenderer.Rendering.Materials
{
	public class Emissive : Diffuse
	{
		public Emissive() => Albedo = Float3.one;
	}
}