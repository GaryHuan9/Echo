using CodeHelpers.Mathematics;
using EchoRenderer.UI.Core.Interactions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace EchoRenderer.UI.Core.Areas
{
	public class RootUI : AreaUI, IHoverable
	{
		public RootUI(Application application)
		{
			this.application = application;

			application.Resized += OnResize;
			application.MouseMoved += OnMouseMoved;
			application.MouseButtonPressed += OnMouseButtonPressed;
			application.MouseButtonReleased += OnMouseButtonReleased;
		}

		public readonly Application application;

		public IHoverable MouseHovering { get; private set; }
		public IHoverable MousePressing { get; private set; }

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

			if (MouseHovering != touching)
			{
				touching?.OnMouseHovered(new MouseHover(args, MouseHover.Type.enter));
				MouseHovering?.OnMouseHovered(new MouseHover(args, MouseHover.Type.exit));

				MouseHovering = touching;
			}
			else MouseHovering?.OnMouseHovered(new MouseHover(args, MouseHover.Type.roam));
		}

		void OnMouseButtonPressed(object sender, MouseButtonEventArgs args)
		{
			if (MouseHovering == null) return;
			MousePressing = MouseHovering;

			MousePressing.OnMousePressed(new MousePress(args, MousePress.Type.down));
		}

		void OnMouseButtonReleased(object sender, MouseButtonEventArgs args)
		{
			MousePressing?.OnMousePressed(new MousePress(args, MousePress.Type.up));
			MousePressing = null;
		}
	}
}