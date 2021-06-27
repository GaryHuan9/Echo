using System;
using System.Globalization;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using SFML.Graphics;

namespace EchoRenderer.UI.Core
{
	public record Theme
	{
		public readonly static Theme darkTheme = new()
												 {
													 ContrastColor = GetColor(245, 245, 250),
													 BackgroundColor = GetColor(3, 3, 4),
													 PanelColor = GetColor(12, 12, 14),
													 SpecialColor = GetColor(0.0250f, 0.1416f, 0.3736f),

													 HoverColor = GetColor(10, 10, 11),
													 PressColor = GetColor(8, 8, 9),

													 SmallMargin = 2f,
													 MediumMargin = 5f,
													 LargeMargin = 9f,
													 LayoutHeight = 18f,

													 Culture = CultureInfo.InvariantCulture
												 };

		//TODO: Add light theme (eww)

		public static Theme Current { get; set; } = darkTheme;

		public Color ContrastColor { get; init; }
		public Color BackgroundColor { get; init; }
		public Color PanelColor { get; init; }
		public Color SpecialColor { get; init; }

		public Color HoverColor { get; init; }
		public Color PressColor { get; init; }

		public float SmallMargin { get; init; }
		public float MediumMargin { get; init; }
		public float LargeMargin { get; init; }
		public float LayoutHeight { get; init; }

		public CultureInfo Culture { get; init; }

		static Color GetColor(byte r, byte g, byte b) => new Color(r, g, b);

		static Color GetColor(Color32 color) => GetColor(color.r, color.g, color.b);

		static Color GetColor(Float4 color) => GetColor((Color32)color);

		static Color GetColor(float r, float g, float b) => GetColor(new Color32(r, g, b));
		static Color GetColor(ReadOnlySpan<char> hex) => GetColor(Utilities.ToColor(hex));
	}
}