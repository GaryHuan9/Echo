using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Echo.Core.Common;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Compute.Statistics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.InOut;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public class OperationUI : AreaUI
{
	public OperationUI() : base("Operation") { }

	int operationIndex;
	Operation lastOperation;

	EventRow[] eventRows;
	readonly List<string> operationLabels = new();
	readonly WorkerData workerData = new();

	public Operation SelectedOperation
	{
		get
		{
			var device = Device.Instance;
			if (device == null) return null;
			var operations = device.PastOperations;
			if (operations.Length == 0) return null;
			return operations[operationIndex];
		}
	}

	protected override void Update(in Moment moment)
	{
		var device = Device.Instance;
		var operations = device == null ? ReadOnlySpan<Operation>.Empty : device.PastOperations;

		if (operations.Length == 0)
		{
			operationIndex = 0;
			lastOperation = null;
			return;
		}

		//Populate operation labels
		if (operations.Length != operationLabels.Count)
		{
			operationLabels.Clear();

			foreach (Operation operation in operations)
			{
				string creation = operation.creationTime.ToInvariant();
				operationLabels.Add($"{operation.GetType().Name} ({creation})");
			}

			operationIndex = operations.Length - 1;
		}

		//Draw operation selector
		ImGuiCustom.Selector("Select", CollectionsMarshal.AsSpan(operationLabels), ref operationIndex);

		Operation selected = operations[operationIndex];

		if (lastOperation != selected)
		{
			LogList.Add($"Changed selected operation in {nameof(OperationUI)}.");
			lastOperation = selected;
		}

		//Draw properties
		double progress = selected.Progress;
		TimeSpan time = selected.Time;

		DrawMain(progress, selected, time);
		DrawEventRows(selected, time, progress);
		DrawWorkers(selected);
	}

	void DrawMain(double progress, Operation operation, TimeSpan time)
	{
		if (!ImGuiCustom.BeginProperties("Main")) return;

		ImGuiCustom.Property("Progress", progress.ToInvariantPercent());
		ImGuiCustom.Property("Completed", operation.IsCompleted.ToString());
		ImGuiCustom.Property("Creation Time", operation.creationTime.ToInvariant());
		ImGuiCustom.Property("Total Workload", operation.totalProcedureCount.ToInvariant());

		ImGui.NewLine();

		ImGuiCustom.Property("Time Spent", time.ToInvariant());
		ImGuiCustom.Property("Time Spent (All Worker)", operation.TotalTime.ToInvariant());

		if (progress.AlmostEquals() || progress.AlmostEquals(1d))
		{
			ImGuiCustom.Property("Estimated Time Remain", "Unavailable");
			ImGuiCustom.Property("Estimated Completion Time", "Unavailable");
		}
		else
		{
			TimeSpan timeRemain = time / progress - time;
			DateTime timeFinish = DateTime.Now + timeRemain;

			ImGuiCustom.Property("Estimated Time Remain", timeRemain.ToInvariant());
			ImGuiCustom.Property("Estimated Completion Time", timeFinish.ToInvariant());
		}

		ImGuiCustom.EndProperties();
	}

	void DrawEventRows(Operation operation, TimeSpan time, double progress)
	{
		ImGui.SetNextItemOpen(true, ImGuiCond.Once);
		if (!ImGui.CollapsingHeader("Events")) return;

		if (operation.EventRowCount <= 0)
		{
			ImGui.TextUnformatted("No event found.");
			return;
		}

		if (!ImGui.BeginTable("Events Table", 4, ImGuiCustom.DefaultTableFlags)) return;

		//Gather information
		double timeR = 1d / time.TotalSeconds;
		double progressR = 1d / progress;
		bool divideByZero = time == TimeSpan.Zero || progress.AlmostEquals();

		Utility.EnsureCapacity(ref eventRows, operation.EventRowCount);

		SpanFill<EventRow> fill = eventRows;
		operation.FillEventRows(ref fill);

		//Draw
		ImGui.TableSetupColumn("Label");
		ImGui.TableSetupColumn("Total Done");
		ImGui.TableSetupColumn("Per Second");
		ImGui.TableSetupColumn("Estimate");
		ImGui.TableHeadersRow();

		foreach ((string label, ulong count) in fill.Filled)
		{
			ImGuiCustom.TableItem(label);
			ImGuiCustom.TableItem(count.ToInvariant());

			if (divideByZero)
			{
				ImGuiCustom.TableItem("Unavailable");
				ImGuiCustom.TableItem("Unavailable");
			}
			else
			{
				ImGuiCustom.TableItem(((float)(count * timeR)).ToInvariant());
				ImGuiCustom.TableItem(((ulong)(count * progressR)).ToInvariant());
			}
		}

		ImGui.EndTable();
	}

	void DrawWorkers(Operation operation)
	{
		ImGui.SetNextItemOpen(true, ImGuiCond.Once);
		if (!ImGui.CollapsingHeader("Workers")) return;

		if (operation.WorkerCount <= 0)
		{
			ImGui.TextUnformatted("No worker found.");
			return;
		}

		if (!ImGui.BeginTable("Workers Table", 4, ImGuiCustom.DefaultTableFlags)) return;

		//Gather data
		workerData.EnsureCapacity(operation.WorkerCount);
		workerData.FillAll(operation);

		//Draw
		ImGui.TableSetupColumn("Guid");
		ImGui.TableSetupColumn("Time Spent");
		ImGui.TableSetupColumn("Procedure Index");
		ImGui.TableSetupColumn("Procedure Progress");
		ImGui.TableHeadersRow();

		for (int i = 0; i < workerData.Length; i++)
		{
			(Guid guid, TimeSpan time, Procedure procedure) = workerData[i];

			ImGuiCustom.TableItem(guid.ToInvariantShort());
			ImGuiCustom.TableItem(time.ToInvariant());
			ImGuiCustom.TableItem(procedure.index.ToInvariant());
			ImGuiCustom.TableItem(procedure.Progress.ToInvariantPercent());
		}

		ImGuiCustom.EndProperties();
	}

	class WorkerData
	{
		int capacity;

		public int Length { get; private set; }

		Guid[] guids = Array.Empty<Guid>();
		TimeSpan[] times = Array.Empty<TimeSpan>();
		Procedure[] procedures = Array.Empty<Procedure>();

		public (Guid guid, TimeSpan time, Procedure procedure) this[int index] => (guids[index], times[index], procedures[index]);

		public void EnsureCapacity(int value)
		{
			if (value < capacity) return;

			capacity = Math.Max(capacity, 16);
			while (capacity < value) capacity *= 2;

			guids = new Guid[capacity];
			times = new TimeSpan[capacity];
			procedures = new Procedure[capacity];
		}

		public void FillAll(Operation operation)
		{
			var guidsFill = guids.AsFill();
			var timesFill = times.AsFill();
			var proceduresFill = procedures.AsFill();

			operation.FillWorkerGuids(ref guidsFill);
			operation.FillWorkerTimes(ref timesFill);
			operation.FillWorkerProcedures(ref proceduresFill);

			Int3 lengths = new Int3(guidsFill.Count, timesFill.Count, proceduresFill.Count);
			if (lengths.Sum != lengths.X * 3) throw new InvalidOperationException("Internal.");

			Length = lengths.X;
		}
	}
}