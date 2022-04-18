using System;

namespace Echo.UserInterface.Core.Areas;

public class ButtonUI : PressableUI
{
	public ButtonUI(string text = "Button")
	{
		label = new LabelUI
		{
			transform =
			{
				UniformMargins = Theme.SmallMargin
			},
			Text = text
		};

		Add(label);
	}

	public readonly LabelUI label;
	public event Action OnPressedMethods;

	protected override void OnMousePressed()
	{
		OnPressedMethods?.Invoke();
	}
}