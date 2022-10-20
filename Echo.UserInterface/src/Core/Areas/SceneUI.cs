using Echo.Core.Evaluation.Operation;
using Echo.Core.InOut;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;

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
		if (operationUI.SelectedOperation is not EvaluationOperation { scene: { } scene }) return;

		// var info = scene.info;
		// var lights = scene.lights;

		if (ImGuiCustom.BeginProperties("Info"))
		{
			// ImGuiCustom.Property("Maximum Depth", info.depth.ToInvariant());
			ImGuiCustom.Property("Enclosing Box", scene.accelerator.BoxBound.ToInvariant());
			ImGuiCustom.Property("Enclosing Sphere", scene.accelerator.SphereBound.ToInvariant());
			// ImGuiCustom.Property("Material Count", info.materialCount.ToInvariant());
			// ImGuiCustom.Property("Entity Pack Count", info.entityPackCount.ToInvariant());

			ImGuiCustom.EndProperties();
		}

		if (ImGuiCustom.BeginProperties("Lights"))
		{
			// ImGuiCustom.Property("Scene Light Count", lights.All.Length.ToInvariant());
			// ImGuiCustom.Property("Ambient Light Count", lights.Ambient.Length.ToInvariant());
			// ImGuiCustom.Property("Total Emissive Power", lights.TotalPower.ToInvariant());
			// ImGuiCustom.Property("Geometry Lights Power", lights.GeometryPower.ToInvariant());

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
		// 	ImGuiCustom.TableItem(info.uniqueCounts.triangle.ToInvariant());
		// 	ImGuiCustom.TableItem(info.uniqueCounts.instance.ToInvariant());
		// 	ImGuiCustom.TableItem(info.uniqueCounts.sphere.ToInvariant());
		//
		// 	ImGuiCustom.TableItem("Instanced");
		// 	ImGuiCustom.TableItem(info.instancedCounts.triangle.ToInvariant());
		// 	ImGuiCustom.TableItem(info.instancedCounts.instance.ToInvariant());
		// 	ImGuiCustom.TableItem(info.instancedCounts.sphere.ToInvariant());
		//
		// 	ImGui.EndTable();
		// }
	}
}