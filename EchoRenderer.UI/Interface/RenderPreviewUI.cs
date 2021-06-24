using EchoRenderer.Textures;
using EchoRenderer.UI.Core;
using EchoRenderer.UI.Core.Areas;
using SFML.Graphics;
using Texture = SFML.Graphics.Texture;

namespace EchoRenderer.UI.Interface
{
	public class RenderPreviewUI : AreaUI
	{
		public RenderPreviewUI()
		{
			imageUI = new ImageUI {Shader = Shader.FromString(null, null, GammaCorrectShader), KeepAspect = false};

			Add(imageUI);
		}

		DisplayBuffer _renderBuffer;

		public DisplayBuffer RenderBuffer
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

		public bool sRGB { get; set; } = true;

		readonly ImageUI imageUI;

		const string GammaCorrectShader = @"

uniform sampler2D texture;
uniform bool sRGB;

void main()
{
    //Fetch pixel color data
    vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);

	//Clean up data & sRGB
	pixel = clamp(pixel, 0.0, 1.0);
	if (sRGB) pixel = sqrt(pixel);

    //Output
    gl_FragColor = gl_Color * pixel;
}

";

		public override void Update()
		{
			base.Update();

			var buffer = RenderBuffer;
			if (buffer == null) return;

			imageUI.Shader.SetUniform("sRGB", sRGB);
			imageUI.Texture.Update(buffer.bytes);
		}
	}
}