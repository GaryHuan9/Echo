using CodeHelpers.Mathematics;
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
			imageUI = new ImageUI();
			Add(imageUI);
		}

		RenderBuffer _renderBuffer;

		public RenderBuffer RenderBuffer
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
					bytesArray = new byte[width * height * 9];
				}

				_renderBuffer = value;
			}
		}

		readonly ImageUI imageUI;
		byte[] bytesArray;

		public override void Update()
		{
			base.Update();
			if (RenderBuffer == null) return;

			foreach (Int2 position in RenderBuffer.size.Loop())
			{
				Color32 pixel = (Color32)RenderBuffer[position];
				int index = RenderBuffer.ToIndex(position) * 4;

				bytesArray[index + 0] = pixel.r;
				bytesArray[index + 1] = pixel.g;
				bytesArray[index + 2] = pixel.b;
				bytesArray[index + 3] = byte.MaxValue;
			}

			imageUI.Texture.Update(bytesArray);
		}
	}
}