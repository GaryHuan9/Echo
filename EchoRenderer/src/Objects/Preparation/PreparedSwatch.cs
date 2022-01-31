using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Objects.Preparation
{
	public class PreparedSwatch
	{
		public PreparedSwatch(Material[] materials) => this.materials = materials;

		readonly Material[] materials;

		//TODO: add emissive material detection and handling

		public Material this[uint token] => materials[token];
	}
}