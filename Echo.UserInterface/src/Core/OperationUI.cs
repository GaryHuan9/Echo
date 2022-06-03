using System;
using CodeHelpers.Mathematics;
using Echo.Common.Compute;
using Echo.Common.Compute.Statistics;
using Echo.Common.Memory;
using ImGuiNET;

namespace Echo.UserInterface.Core;

public class OperationUI : AreaUI
{
	public OperationUI() : base("Operation") { }

	EventRow[] rows = Array.Empty<EventRow>();

	protected override void Draw()
	{
		var device = Device.Instance;
		var operation = device?.StartedOperation;
		if (operation == null) return;

		double timeR = 1d / device.StartedTime.TotalSeconds;
		double progressR = 1d / device.StartedProgress;
		bool unavailable = timeR.AlmostEquals() || progressR.AlmostEquals();

		//Main properties
		if (ImGuiCustom.BeginProperties("Main"))
		{
			ImGuiCustom.Property("Completed", operation.IsCompleted.ToString());
			ImGuiCustom.Property("Total Workload", operation.TotalProcedureCount.ToStringDefault());
			ImGuiCustom.Property("Completed Work", operation.CompletedProcedureCount.ToStringDefault());

			ImGuiCustom.EndProperties();
		}

		//Event properties
		if (operation.EventCount > 0 && ImGui.BeginTable("Events", 4, ImGuiTableFlags.BordersOuter))
		{
			//Gather the data
			if (rows.Length < operation.EventCount) rows = new EventRow[operation.EventCount];

			SpanFill<EventRow> fill = rows.AsFill();
			operation.FillEventRows(ref fill);

			//Draw
			ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
			ImGuiCustom.TableItem("Label");
			ImGuiCustom.TableItem("Total Done");
			ImGuiCustom.TableItem("Per Second");
			ImGuiCustom.TableItem("Estimate");

			foreach ((string label, ulong count) in fill.Filled)
			{
				ImGuiCustom.TableItem(label);
				ImGuiCustom.TableItem(count.ToStringDefault());

				if (unavailable)
				{
					ImGuiCustom.TableItem("Unavailable");
					ImGuiCustom.TableItem("Unavailable");
				}
				else
				{
					ImGuiCustom.TableItem((count * timeR).ToStringDefault());
					ImGuiCustom.TableItem((count * progressR).ToStringDefault());
				}
			}

			ImGuiCustom.EndProperties();
		}
	}
}