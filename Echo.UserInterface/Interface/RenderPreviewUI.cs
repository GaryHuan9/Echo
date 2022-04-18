using Echo.Core.Textures.Grid;
using Echo.UserInterface.Core.Areas;
using Echo.UserInterface.Core;
using SFML.Graphics;

namespace Echo.UserInterface.Interface;

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
				uint width = (uint)value.size.X;
				uint height = (uint)value.size.Y;

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