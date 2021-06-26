using EchoRenderer.Objects.Scenes;
using EchoRenderer.UI.Core.Areas;

namespace EchoRenderer.UI.Interface
{
	public class HierarchyUI : WindowUI
	{
		public HierarchyUI() : base("Hierarchy")
		{
			transform.RightPercent = 0.84f;

			rebuildButton = new ButtonUI {label = {Text = "Rebuild Scene"}};
			rebuildButton.OnPressedMethods += OnRebuildPressed;

			group.Add(rebuildButton);
		}

		readonly ButtonUI rebuildButton;

		Scene opened;

		HierarchyNodeUI root;

		public override void Update()
		{
			base.Update();

			SceneViewUI sceneView = Root.Find<SceneViewUI>();
			Scene scene = sceneView?.Profile.Scene?.source;

			if (scene == opened) return;

			if (opened != null && root != null && group.Contains(root)) group.Remove(root);

			root = new HierarchyNodeUI(scene);

			opened = scene;
			group.Add(root);
		}

		void OnRebuildPressed() { }
	}
}