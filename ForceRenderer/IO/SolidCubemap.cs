using CodeHelpers;
using CodeHelpers.Vectors;

namespace ForceRenderer.IO
{
	public class SolidCubemap : Cubemap
	{
		public SolidCubemap(Color32 ambient) => this.ambient = ambient;

		public readonly Color32 ambient;

		public override Color32 Sample(Float3 direction) => ambient;
	}
}