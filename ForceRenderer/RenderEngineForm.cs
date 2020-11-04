using Eto.Drawing;
using Eto.Forms;

namespace ForceRenderer
{
	public class RenderEngineForm : Form
	{
		public RenderEngineForm()
		{
			Title = "My Cross-Platform App";
			ClientSize = new Size(200, 200);
			Content = new Label {Text = "Hello World!"};
		}
	}
}