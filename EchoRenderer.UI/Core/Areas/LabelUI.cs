using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using SFML.Graphics;

namespace EchoRenderer.UI.Core.Areas
{
	public class LabelUI : AreaUI
	{
		readonly Text display = new Text("Hello World", mono);

		static readonly Font mono = new Font("Assets/Fonts/JetBrainsMono/JetBrainsMono-Bold.ttf");

		protected override void Reorient(Float2 position, Float2 size)
		{
			base.Reorient(position, size);

			display.CharacterSize = (uint)size.y;
			display.Position = position.As();
		}

		protected override void Paint(RenderTarget renderTarget)
		{
			base.Paint(renderTarget);
			renderTarget.Draw(display);

			// DebugHelper.Log(display.GetLocalBounds());
		}
	}
}