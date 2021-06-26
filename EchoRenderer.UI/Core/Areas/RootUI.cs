using System;
using System.Collections.Generic;
using CodeHelpers;
using CodeHelpers.Diagnostics;
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

			application.MouseWheelScrolled += OnMouseWheelScrolled;
		}

		public readonly Application application;

		public IHoverable MouseHovering { get; private set; }
		public IHoverable MousePressing { get; private set; }

		readonly Dictionary<Type, AreaUI> typeMapper = new Dictionary<Type, AreaUI>();

		public void Resize(Float2 size)
		{
			Reorient(Float2.zero, size);
			transform.MarkDirty();
		}

		public T Find<T>() where T : AreaUI => (T)Find(typeof(T));

		public AreaUI Find(Type type)
		{
			if (typeMapper.TryGetValue(type, out AreaUI value))
			{
				if (value.Root == this) return value;
				typeMapper.Remove(type);
			}

			AreaUI result = Search(this);
			if (result == null) return null;

			typeMapper[type] = result;
			return result;

			AreaUI Search(AreaUI current)
			{
				if (type.IsInstanceOfType(current)) return current;

				foreach (AreaUI child in current!)
				{
					AreaUI search = Search(child);
					if (search == null) continue;

					return search;
				}

				return null;
			}
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

		void OnMouseWheelScrolled(object sender, MouseWheelScrollEventArgs args)
		{
			int axis = args.Wheel switch
					   {
						   Mouse.Wheel.HorizontalWheel => 0,
						   Mouse.Wheel.VerticalWheel => 1,
						   _ => throw ExceptionHelper.Invalid(nameof(args.Wheel), args.Wheel, InvalidType.unexpected)
					   };

			MouseHovering?.OnMouseScrolled(Float2.Create(axis, args.Delta));
		}
	}
}