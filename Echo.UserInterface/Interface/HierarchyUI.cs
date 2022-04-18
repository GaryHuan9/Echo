using System.Collections.Generic;
using Echo.Core.Scenic.Instancing;
using Echo.UserInterface.Core.Areas;

namespace Echo.UserInterface.Interface;

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

	readonly HashSet<EntityPack> packs = new();
	readonly List<HierarchyNodeUI> nodes = new();

	// public override void Update()
	// {
	// 	base.Update();
	//
	// 	SceneViewUI sceneView = Root.Find<SceneViewUI>();
	// 	Scene scene = sceneView?.Profile.Scene?.source;
	//
	// 	if (scene == null || packs.Contains(scene)) return;
	//
	// 	foreach (HierarchyNodeUI node in nodes) group.Remove(node);
	//
	// 	packs.Clear();
	// 	nodes.Clear();
	//
	// 	ScenePreparer preparer = sceneView.Profile.Scene.preparer;
	//
	// 	foreach (EntityPack pack in preparer.UniquePacks)
	// 	{
	// 		Assert.IsFalse(packs.Contains(pack));
	// 		var node = new HierarchyNodeUI(pack);
	//
	// 		packs.Add(pack);
	// 		nodes.Add(node);
	// 		group.Add(node);
	// 	}
	// }

	void OnRebuildPressed() { }
}