using CodeHelpers.Mathematics;

namespace ForceRenderer.Rendering.Materials
{
	public class Emissive : Diffuse
	{
		public Emissive() => Albedo = Float3.one;
	}
}