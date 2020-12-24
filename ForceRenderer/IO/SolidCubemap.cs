using CodeHelpers.Mathematics;

namespace ForceRenderer.IO
{
	public class SolidCubemap : Cubemap
	{
		public SolidCubemap(Color32 ambient) => this.ambient = (Float3)ambient;

		public readonly Float3 ambient;

		public override Float3 Sample(Float3 direction) => ambient;
	}
}