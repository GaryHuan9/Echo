using CodeHelpers.Mathematics;

namespace ForceRenderer.Textures
{
	public class SolidCubemap : Cubemap
	{
		public SolidCubemap(Float3 ambient) => this.ambient = ambient;

		public readonly Float3 ambient;

		public override Float3 Sample(Float3 direction) => ambient;
	}
}