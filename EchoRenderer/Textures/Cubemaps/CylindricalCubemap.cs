using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Textures.Cubemaps
{
	public class CylindricalCubemap : Cubemap
	{
		public CylindricalCubemap(string path) => texture = Texture2D.Load(path);

		readonly Texture2D texture;

		public override Float3 Sample(in Float3 direction) => Utilities.ToFloat4
		(
			texture
			[
				new Float2
				(
					MathF.Atan2(direction.x, -direction.z) / -Scalars.PI * 0.5f,
					MathF.Acos(direction.y) / -Scalars.PI
				)
			]
		).XYZ;
	}
}