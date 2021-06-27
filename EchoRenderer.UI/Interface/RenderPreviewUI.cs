using EchoRenderer.Textures;
using EchoRenderer.UI.Core;
using EchoRenderer.UI.Core.Areas;
using Texture = SFML.Graphics.Texture;

namespace EchoRenderer.UI.Interface
{
	public class RenderPreviewUI : AreaUI
	{
		public RenderPreviewUI()
		{
			imageUI = new ImageUI {KeepAspect = false};

			Add(imageUI);
		}

		ProgressiveRenderBuffer _renderBuffer;

		public ProgressiveRenderBuffer RenderBuffer
		{
			get => _renderBuffer;
			set
			{
				if (_renderBuffer == value) return;
				Texture texture = imageUI.Texture;

				if (texture != null && texture.Size.Cast() != value.size)
				{
					texture.Dispose();
					texture = null;
				}

				if (texture == null)
				{
					uint width = (uint)value.size.x;
					uint height = (uint)value.size.y;

					imageUI.Texture = new Texture(width, height);
				}

				_renderBuffer = value;
			}
		}

		readonly ImageUI imageUI;

		public override void Update()
		{
			base.Update();

			var buffer = RenderBuffer;
			if (buffer == null) return;

			imageUI.Texture.Update(buffer.bytes);
		}
	}
}