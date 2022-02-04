using EchoRenderer.UserInterface.Core.Areas;

namespace EchoRenderer.UserInterface.Interface
{
	public class WindowUI : AreaUI
	{
		public WindowUI(string name)
		{
			transform.UniformMargins = Theme.LargeMargin;

			title = new ButtonUI
					{
						transform =
						{
							BottomPercent = 1f,
							BottomMargin = -Theme.LayoutHeight
						},
						label = {Text = name}
					};

			group = new AutoLayoutAreaUI
					{
						transform = {TopMargin = Theme.LayoutHeight + Theme.MediumMargin},
						PanelColor = Theme.BackgroundColor
					};

			Add(title);
			Add(group);

			title.OnPressedMethods += OnTitlePressed;
		}

		readonly ButtonUI title;

		protected readonly AutoLayoutAreaUI group;

		void OnTitlePressed()
		{
			group.Enabled = !group.Enabled;
		}
	}
}