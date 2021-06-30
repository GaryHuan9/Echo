using System.Collections.Generic;
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
		readonly HashSet<HierarchyNodeUI> nodes = new();

		public override void Update()
		{
			base.Update();

			SceneViewUI sceneView = Root.Find<SceneViewUI>();
			Scene scene = sceneView?.Profile.Scene?.source;

			if (scene == null || packs.Contains(scene)) return;

			foreach (HierarchyNodeUI node in nodes) group.Remove(node);

			nodes.Clear();

			Queue<ObjectPack> frontier = new Queue<ObjectPack>();

			frontier.Enqueue(scene);

			while (frontier.Count > 0)
			{
				ObjectPack pack = frontier.Dequeue();

				if (packs.Contains(pack))
				{
					//TODO: Invalid pack ordering (recursive parenting)
					continue;
				}

				var node = new HierarchyNodeUI(pack);

				foreach (ObjectPack child in node.packs) frontier.Enqueue(child);

				packs.Add(pack);
				nodes.Add(node);
				group.Add(node);
			}
		}

		void OnRebuildPressed() { }
	}
}