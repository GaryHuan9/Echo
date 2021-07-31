using System.Collections.Generic;
using CodeHelpers.Diagnostics;
using EchoRenderer.Objects;
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

		readonly HashSet<ObjectPack> packs = new();
		readonly List<HierarchyNodeUI> nodes = new();

		public override void Update()
		{
			base.Update();

			SceneViewUI sceneView = Root.Find<SceneViewUI>();
			Scene scene = sceneView?.Profile.Scene?.source;

			if (scene == null || packs.Contains(scene)) return;

			foreach (HierarchyNodeUI node in nodes) group.Remove(node);

			packs.Clear();
			nodes.Clear();

			ScenePresser presser = sceneView.Profile.Scene.presser;

			foreach (ObjectPack pack in presser.UniquePacks)
			{
				Assert.IsFalse(packs.Contains(pack));
				var node = new HierarchyNodeUI(pack);

				packs.Add(pack);
				nodes.Add(node);
				group.Add(node);
			}
		}

		void OnRebuildPressed() { }
	}
}