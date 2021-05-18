using CodeHelpers.Mathematics;
using SFML.Graphics;

namespace EchoRenderer.UI.Core.Areas
{
	public class ImageUI : AreaUI
	{
		public bool KeepAspect { get; set; } = true;

		public Texture Texture { get; set; }
		public Shader Shader { get; set; }

		readonly RectangleShape display = new RectangleShape();

		protected override void Reorient(Float2 position, Float2 size)
		{
			base.Reorient(position, size);
			if (Texture == null) return;

			if (KeepAspect)
			{
				float aspect = (float)Texture.Size.X / Texture.Size.Y;
				int majorAxis = aspect * size.y / size.x > 1f ? 0 : 1;

				Float2 center = position + size / 2f;
				float border = size[majorAxis];

				size = new Float2(border * aspect, border);
				if (majorAxis == 0) size /= aspect;

				position = center - size / 2f;
			}

			display.Position = position.As();
			display.Size = size.As();
		}

		protected override void Paint(RenderTarget renderTarget)
		{
			base.Paint(renderTarget);
			if (Texture == null) return;

			display.Texture = Texture;

			if (Shader != null && Shader.IsAvailable)
			{
				Shader.SetUniform("texture", Shader.CurrentTexture);
				renderTarget.Draw(display, new RenderStates(Shader));
			}
			else renderTarget.Draw(display);
		}
	}
}