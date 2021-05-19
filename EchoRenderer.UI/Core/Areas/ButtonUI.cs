using System;

namespace EchoRenderer.UI.Core.Areas
{
	public class ButtonUI : PressableUI
	{
		public ButtonUI()
		{
			label = new LabelUI {transform = {UniformMargins = Theme.SmallMargin}};
			Add(label);
		}

		public readonly LabelUI label;
		public event Action OnPressed;

		protected override void OnMousePressed()
		{
			OnPressed?.Invoke();
		}
	}
}