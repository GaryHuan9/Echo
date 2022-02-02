using EchoRenderer.Common;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Scenic.Preparation
{
	public class PreparedSwatch
	{
		public PreparedSwatch(Material[] materials)
		{
			this.materials = materials;

			foreach (var material in materials)
			{
				if (!material.Emission.PositiveRadiance()) continue;

				// material.

				hasEmission = true;
				break;
			}
		}

		readonly Material[] materials;
		readonly bool hasEmission;

		//TODO: add emissive material detection and handling

		public Material this[uint token] => materials[token];
	}
}