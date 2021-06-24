using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using SFML.Graphics;
using SFML.System;

namespace EchoRenderer.UI.Core.Areas
{
	public class ImageUI : AreaUI
	{
		bool _keepAspect = true;
		Texture _texture;

		public bool KeepAspect
		{
			get => _keepAspect;
			set
			{
				if (_keepAspect == value) return;

				_keepAspect = value;
				transform.MarkDirty();
			}
		}

		public Texture Texture
		{
			get => _texture;
			set
			{
				if (_texture == value) return;
				_texture = value;

				transform.MarkDirty();
				display.Texture = value;

				Int2 size = value.Size.Cast();
				display.TextureRect = new IntRect(0, 0, size.x, size.y);
			}
		}

		public Shader Shader { get; set; }

		public Color ImageColor
		{
			get => display.FillColor;
			set => display.FillColor = value;
		}

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

			if (Shader != null && Shader.IsAvailable)
			{
				Shader.SetUniform("texture", Shader.CurrentTexture);
				renderTarget.Draw(display, new RenderStates(Shader));
			}
			else renderTarget.Draw(display);
		}
	}
}