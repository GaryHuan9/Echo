using System;
using CodeHelpers.Mathematics;

namespace ForceRenderer.Textures
{
	public class CylindricalCubemap : Cubemap
	{
		public CylindricalCubemap(string path) => texture = Texture2D.Load(path);

		readonly Texture2D texture;

		public override Float3 Sample(Float3 direction) => texture
		[
			new Float2
			(
				(float)Math.Atan2(direction.x, -direction.z) / -Scalars.PI * 0.5f,
				(float)Math.Acos(direction.y) / -Scalars.PI
			)
		];
	}
}