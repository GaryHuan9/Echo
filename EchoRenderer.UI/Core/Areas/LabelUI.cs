using System;
using CodeHelpers.Mathematics;
using SFML.Graphics;

namespace EchoRenderer.UI.Core.Areas
{
	public class LabelUI : AreaUI
	{
		public string Text
		{
			get => display.DisplayedString;
			set => display.DisplayedString = value;
		}

		public Text.Styles Styles
		{
			get => display.Style;
			set => display.Style = value;
		}

		public override Color FillColor
		{
			get => display.FillColor;
			set => display.FillColor = value;
		}

		readonly Text display = new Text {Font = mono, FillColor = Theme.Current.ContrastColor};
		static readonly Font mono = new Font("Assets/Fonts/JetBrainsMono/JetBrainsMono-Bold.ttf");

		protected override void Reorient(Float2 position, Float2 size)
		{
			base.Reorient(position, size);

			display.CharacterSize = (uint)Math.Max(0, size.y);

			var bounds = display.GetLocalBounds();
			Float2 center = position + size / 2f;

			Float2 offset = new Float2(bounds.Left, bounds.Top);
			Float2 extend = new Float2(bounds.Width, bounds.Height) / 2f;

			display.Origin = (offset + extend).As();
			display.Position = center.As();
		}

		protected override void Paint(RenderTarget renderTarget)
		{
			base.Paint(renderTarget);
			renderTarget.Draw(display);
		}
	}
}