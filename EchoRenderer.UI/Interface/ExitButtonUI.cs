using EchoRenderer.UI.Core.Areas;

namespace EchoRenderer.UI.Interface
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