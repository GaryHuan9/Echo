using EchoRenderer.UserInterface.Core.Areas;

namespace EchoRenderer.UserInterface.Interface
{
	public class ExitButtonUI : ButtonUI
	{
		public ExitButtonUI() => label.Text = "Exit";

		protected override void OnMousePressed()
		{
			base.OnMousePressed();
			Root.application.Close();
		}
	}
}