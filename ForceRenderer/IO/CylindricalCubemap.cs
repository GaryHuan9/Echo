using System;
using CodeHelpers;
using CodeHelpers.Vectors;

namespace ForceRenderer.IO
{
	public class CylindricalCubemap : Cubemap
	{
		public CylindricalCubemap(string path)
		{
			this.path = path;
			texture = new Texture(path);
		}

		public readonly string path;
		readonly Texture texture;

		public override Color32 Sample(Float3 direction) => texture.GetPixel
		(
			new Float2
			(
				(float)Math.Atan2(direction.x, -direction.z) / -Scalars.PI * 0.5f,
				(float)Math.Acos(direction.y) / -Scalars.PI
			).Repeat(1f)
		);
	}
}