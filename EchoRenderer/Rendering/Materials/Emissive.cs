using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.Materials
{
	public class Emissive : Diffuse
	{
		public Emissive() => Albedo = Float3.one;
	}
}