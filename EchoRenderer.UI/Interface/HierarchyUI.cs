using EchoRenderer.UI.Core.Areas;
using EchoRenderer.UI.Core.Fields;

namespace EchoRenderer.UI.Interface
{
	public class HierarchyUI : AreaUI
	{
		public HierarchyUI()
		{
			transform.RightPercent = 0.8f;
			transform.UniformMargins = 10f;

			Add
			(
				new AutoLayoutAreaUI { }.Add
				(
					new ButtonUI
					{
						label = {Text = "Button"}
					}.Label("Click Me")
				).Add
				(
					new TextFieldUI {Text = "Test Field Here"}.Label("Type me")
				).Add
				(
					new FloatFieldUI { }.Label("Sample Count")
				)
			);
		}
	}
}