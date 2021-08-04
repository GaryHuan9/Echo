﻿using EchoRenderer.Objects;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.UI.Core.Areas;

namespace EchoRenderer.UI.Interface
{
	public class HierarchyNodeUI : AutoLayoutAreaUI
	{
		public HierarchyNodeUI(Object nodeObject)
		{
			this.nodeObject = nodeObject;
			float height = Theme.LayoutHeight;

			selectButton = new ButtonUI(nodeObject.Name)
						   {
							   label =
							   {
								   Align = LabelUI.Alignment.left
							   }
						   };

			expandButton = new ButtonUI
						   {
							   transform =
							   {
								   LeftPercent = 1f,
								   LeftMargin = -height
							   }
						   };

			selectButton.OnPressedMethods += OnSelectButtonPressed;
			expandButton.OnPressedMethods += OnExpandButtonPressed;

			container = new AutoLayoutAreaUI
						{
							Margins = false,
							NegativeMargin = height
						};

			Add(selectButton);
			Add(container);

			selectButton.Add(expandButton);
			SetExpanded(nodeObject is Scene);

			Margins = false;

			foreach (Object child in nodeObject.LoopChildren(false)) container.Add(new HierarchyNodeUI(child));
		}

		public readonly Object nodeObject;

		readonly ButtonUI selectButton;
		readonly ButtonUI expandButton;

		readonly AutoLayoutAreaUI container;

		bool expanded;

		public override void Update()
		{
			base.Update();

			if (nodeObject.children.Count == 0)
			{
				expandButton.Enabled = false;
				container.Enabled = false;

				expanded = false;
			}
			else
			{
				expandButton.Enabled = true;
				container.Enabled = expanded;
			}
		}

		void OnSelectButtonPressed() { }

		void OnExpandButtonPressed() => SetExpanded(!expanded);

		void SetExpanded(bool value)
		{
			expanded = value;
			expandButton.label.Text = value ? "-" : "+";
		}
	}
}