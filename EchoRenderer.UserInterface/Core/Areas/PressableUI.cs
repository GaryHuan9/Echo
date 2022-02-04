using CodeHelpers.Mathematics;
using EchoRenderer.UserInterface.Core.Interactions;
using SFML.Graphics;

namespace EchoRenderer.UserInterface.Core.Areas
{
	public abstract class PressableUI : AreaUI, IHoverable
	{
		public PressableUI() => PanelColor = Theme.PanelColor;

		public bool IsHovering { get; private set; }
		public bool IsPressing { get; private set; }

		Color HoverColor => IsHovering ? Theme.HoverColor : Theme.PanelColor;

		public virtual void OnMouseHovered(MouseHover mouse)
		{
			IsHovering = mouse.type != MouseHover.Type.exit;
			if (!IsPressing) PanelColor = HoverColor;
		}

		public virtual void OnMousePressed(MousePress mouse)
		{
			IsPressing = mouse.type == MousePress.Type.down;
			PanelColor = IsPressing ? Theme.PressColor : HoverColor;

			if (IsPressing) OnMousePressed();
		}

		public virtual void OnMouseScrolled(Float2 delta) { }

		protected abstract void OnMousePressed();
	}
}