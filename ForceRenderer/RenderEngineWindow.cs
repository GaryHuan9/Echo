using Avalonia;
using Avalonia.Controls;

namespace ForceRenderer
{
	public class RenderEngineWindow : Window
	{
		public RenderEngineWindow()
		{
			CanResize = true;
			Title = "Hello!";
			Opacity = 1d;

			Height = 100d;
			Width = 100d;
		}
	}
}