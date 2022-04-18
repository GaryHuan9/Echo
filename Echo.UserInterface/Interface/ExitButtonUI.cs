using Echo.UserInterface.Core.Areas;

namespace Echo.UserInterface.Interface;

public class ExitButtonUI : ButtonUI
{
	public ExitButtonUI() => label.Text = "Exit";

	protected override void OnMousePressed()
	{
		base.OnMousePressed();
		Root.application.Close();
	}
}