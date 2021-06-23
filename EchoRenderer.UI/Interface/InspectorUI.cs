using EchoRenderer.UI.Core.Areas;
using EchoRenderer.UI.Core.Fields;

namespace EchoRenderer.UI.Interface
{
	public class InspectorUI : AreaUI
	{
		public InspectorUI()
		{
			transform.LeftPercent = 0.8f;
			transform.UniformMargins = 10f;

			Add
			(
				new AutoLayoutAreaUI { }.Add
				(
					new LabelUI
					{
						Text = "Hello World 1",
						Align = LabelUI.Alignment.left
					}
				).Add
				(
					new ButtonUI
					{
						label =
						{
							Text = "Button 1",
							Align = LabelUI.Alignment.right
						}
					}
				).Add
				(
					new LabelUI
					{
						Text = "Hello World 2 pp"
					}
				).Add
				(
					new ButtonUI
					{
						label = {Text = "Button 2"}
					}
				).Add
				(
					new ButtonUI
					{
						label = {Text = "Button 3"}
					}
				).Add
				(
					new TextFieldUI {Text = "Test Field Hehe"}
				).Add
				(
					new FloatFieldUI { }
				).Add
				(
					new Float3FieldUI { }
				)
			);
		}
	}
}