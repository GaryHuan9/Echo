using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace SFMLdotnet
{
	class Program
	{
		static void Main()
		{
			VideoMode mode = new VideoMode(250, 250);
			RenderWindow window = new RenderWindow(mode, "SFML.NET");

			window.Closed += (obj, e) => { window.Close(); };
			window.KeyPressed +=
				(sender, e) =>
				{
					Window window = (Window)sender;
					if (e.Code == Keyboard.Key.Escape)
					{
						window.Close();
					}
				};

			Font font = new Font("C:/Windows/Fonts/arial.ttf");
			Text text = new Text("Hello World!", font);
			text.CharacterSize = 40;
			float textWidth = text.GetLocalBounds().Width;
			float textHeight = text.GetLocalBounds().Height;
			float xOffset = text.GetLocalBounds().Left;
			float yOffset = text.GetLocalBounds().Top;
			text.Origin = new Vector2f(textWidth / 2f + xOffset, textHeight / 2f + yOffset);
			text.Position = new Vector2f(window.Size.X / 2f, window.Size.Y / 2f);

			Clock clock = new Clock();
			float delta = 0f;
			float angle = 0f;
			float angleSpeed = 90f;

			while (window.IsOpen)
			{
				delta = clock.Restart().AsSeconds();
				angle += angleSpeed * delta;
				window.DispatchEvents();
				window.Clear();
				text.Rotation = angle;

				window.Draw(text);
				window.Display();
			}
		}
	}
}