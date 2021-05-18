using CodeHelpers.Mathematics;
using SFML.Graphics;
using SFML.Window;

namespace EchoRenderer.UI.Core.Areas
{
	public class RootUI : AreaUI
	{
		public RootUI(Application application)
		{
			this.application = application;

			application.Resized += OnResize;
		}

		readonly Application application;

		public void Resize(Float2 size)
		{
			Reorient(Float2.zero, size);
			transform.MarkDirty();
		}

		void OnResize(object sender, SizeEventArgs argument)
		{
			Float2 size = new Float2(argument.Width, argument.Height);
			application.SetView(new View(size.As() / 2f, size.As()));

			Resize(size);
		}
	}
}