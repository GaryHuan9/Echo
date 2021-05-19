using CodeHelpers.Mathematics;
using EchoRenderer.UI.Core.Interactions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace EchoRenderer.UI.Core.Areas
{
	public class RootUI : AreaUI
	{
		public RootUI(Application application)
		{
			this.application = application;

			application.Resized += OnResize;
			application.MouseMoved += OnMouseMoved;
			application.MouseButtonPressed += OnMouseButtonPressed;
			application.MouseButtonReleased += OnMouseButtonReleased;
		}

		readonly Application application;

		IHoverable mouseHovering;
		IHoverable mousePressing;

		public void Resize(Float2 size)
		{
			Reorient(Float2.zero, size);
			transform.MarkDirty();
		}

		void OnResize(object sender, SizeEventArgs args)
		{
			var size = new Vector2f(args.Width, args.Height);
			application.SetView(new View(size / 2f, size));

			Resize(size.As());
		}

		void OnMouseMoved(object sender, MouseMoveEventArgs args)
		{
			IHoverable touching = Find(args.As());

			if (mouseHovering != touching)
			{
				touching?.OnMouseHovered(new MouseHover(args, MouseHover.Type.enter));
				mouseHovering?.OnMouseHovered(new MouseHover(args, MouseHover.Type.exit));

				mouseHovering = touching;
			}
			else mouseHovering?.OnMouseHovered(new MouseHover(args, MouseHover.Type.roam));
		}

		void OnMouseButtonPressed(object sender, MouseButtonEventArgs args)
		{
			if (mouseHovering == null) return;
			mousePressing = mouseHovering;

			mousePressing.OnMousePressed(new MousePress(args, MousePress.Type.down));
		}

		void OnMouseButtonReleased(object sender, MouseButtonEventArgs args)
		{
			mousePressing?.OnMousePressed(new MousePress(args, MousePress.Type.up));
			mousePressing = null;
		}
	}
}