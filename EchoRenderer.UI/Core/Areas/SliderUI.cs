using System;
using EchoRenderer.UI.Core.Interactions;
using SFML.Graphics;

namespace EchoRenderer.UI.Core.Areas
{
	public class SliderUI : AreaUI, IHoverable
	{
		public SliderUI()
		{
			base.FillColor = Theme.PanelColor;

			label = new LabelUI {transform = {UniformMargins = Theme.SmallMargin}};

			Add(label);
		}

		public readonly LabelUI label;
		public event Action<float> OnSlide;

		bool isHovering;
		bool isPressing;

		Color HoverColor => isHovering ? Theme.HoverColor : Theme.PanelColor;

		public void OnMouseHovered(MouseHover mouse)
		{
			isHovering = mouse.type != MouseHover.Type.exit;
			if (!isPressing) FillColor = HoverColor;
		}

		public void OnMousePressed(MousePress mouse)
		{
			isPressing = mouse.type == MousePress.Type.down;
			FillColor = isPressing ? Theme.PressColor : HoverColor;

			// if (isPressing) OnSlide?.Invoke();
		}
	}
}