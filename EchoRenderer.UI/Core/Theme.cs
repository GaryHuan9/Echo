using SFML.Graphics;

namespace EchoRenderer.UI.Core
{
	public class Theme
	{
		public readonly static Theme darkTheme = new()
												 {
													 ContrastColor = new Color(250, 250, 252),
													 BackgroundColor = new Color(26, 26, 30),
													 PanelColor = new Color(55, 55, 59),
													 SpecialColor = new Color(60, 144, 234),

													 HoverColor = new Color(50, 50, 54),
													 PressColor = new Color(42, 42, 45),

													 SmallMargin = 2f,
													 LargeMargin = 7f,
													 LayoutHeight = 24f
												 };

		//TODO: Add light theme (eww)

		public static Theme Current { get; set; } = darkTheme;

		public Color ContrastColor { get; set; }
		public Color BackgroundColor { get; set; }
		public Color PanelColor { get; set; }
		public Color SpecialColor { get; set; }

		public Color HoverColor { get; set; }
		public Color PressColor { get; set; }

		public float SmallMargin { get; set; }
		public float LargeMargin { get; set; }
		public float LayoutHeight { get; set; }
	}
}