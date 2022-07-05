using Echo.Core.Evaluation.Operations;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public class SceneUI : AreaUI
{
	public SceneUI() : base("Scene") { }

	public override void Initialize()
	{
		base.Initialize();
		operationUI = Root.Find<OperationUI>();
	}

	OperationUI operationUI;

	protected override void Update(in Moment moment)
	{
		if (operationUI.SelectedOperation is not EvaluationOperation { profile.Scene: { } scene }) return;

		// var info = scene.info;
		// var lights = scene.lights;

		if (ImGuiCustom.BeginProperties("Info"))
		{
			// ImGuiCustom.Property("Maximum Depth", info.depth.ToStringDefault());
			ImGuiCustom.Property("Enclosing Box", scene.accelerator.BoxBound.ToString(DefaultFormat.Floating));
			ImGuiCustom.Property("Enclosing Sphere", scene.accelerator.SphereBound.ToString(DefaultFormat.Floating));
			// ImGuiCustom.Property("Material Count", info.materialCount.ToStringDefault());
			// ImGuiCustom.Property("Entity Pack Count", info.entityPackCount.ToStringDefault());

			ImGuiCustom.EndProperties();
		}

		if (ImGuiCustom.BeginProperties("Lights"))
		{
			// ImGuiCustom.Property("Scene Light Count", lights.All.Length.ToStringDefault());
			// ImGuiCustom.Property("Ambient Light Count", lights.Ambient.Length.ToStringDefault());
			// ImGuiCustom.Property("Total Emissive Power", lights.TotalPower.ToStringDefault());
			// ImGuiCustom.Property("Geometry Lights Power", lights.GeometryPower.ToStringDefault());

			ImGuiCustom.EndProperties();
		}

		// if (ImGui.BeginTable("Geometries Table", 4, ImGuiCustom.DefaultTableFlags))
		// {
		// 	ImGui.TableSetupColumn("Geometry Kind");
		// 	ImGui.TableSetupColumn("Triangle");
		// 	ImGui.TableSetupColumn("Instance");
		// 	ImGui.TableSetupColumn("Sphere");
		// 	ImGui.TableHeadersRow();
		//
		// 	ImGuiCustom.TableItem("Unique");
		// 	ImGuiCustom.TableItem(info.uniqueCounts.triangle.ToStringDefault());
		// 	ImGuiCustom.TableItem(info.uniqueCounts.instance.ToStringDefault());
		// 	ImGuiCustom.TableItem(info.uniqueCounts.sphere.ToStringDefault());
		//
		// 	ImGuiCustom.TableItem("Instanced");
		// 	ImGuiCustom.TableItem(info.instancedCounts.triangle.ToStringDefault());
		// 	ImGuiCustom.TableItem(info.instancedCounts.instance.ToStringDefault());
		// 	ImGuiCustom.TableItem(info.instancedCounts.sphere.ToStringDefault());
		//
		// 	ImGui.EndTable();
		// }
	}
}