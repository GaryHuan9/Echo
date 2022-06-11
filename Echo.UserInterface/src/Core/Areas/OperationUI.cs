using System;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Common;
using Echo.Common.Compute;
using Echo.Common.Compute.Statistics;
using Echo.Common.Memory;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public class OperationUI : AreaUI
{
	public OperationUI() : base("Operation") { }

	int selectionIndex;
	int lastOperationCount;

	string[] operationLabels;
	EventRow[] eventRows;
	readonly WorkerData workerData = new();

	protected override void UpdateImpl(in Moment moment)
	{
		var device = Device.Instance;
		var operations = device == null ? ReadOnlySpan<Operation>.Empty : device.PastOperations;

		if (operations.Length == 0)
		{
			selectionIndex = 0;
			lastOperationCount = 0;
			return;
		}

		UpdateOperationLabels(operations);

		// ImGui.Combo("Select", ref selectionIndex, operationLabels, lastOperationCount);
		var operation = operations[Math.Min(selectionIndex, lastOperationCount - 1)];

		double progress = operation.Progress;
		TimeSpan time = operation.Time;

		//Draw properties
		DrawMain(progress, operation, time);
		DrawEventRows(operation, time, progress);
		DrawWorkers(operation);
	}

	void UpdateOperationLabels(ReadOnlySpan<Operation> operations)
	{
		if (operations.Length == lastOperationCount) return;
		Utility.EnsureCapacity(ref operationLabels, operations.Length);

		for (int i = 0; i < operations.Length; i++)
		{
			Operation operation = operations[i];
			string creation = operation.creationTime.ToStringDefault();
			operationLabels[i] = $"[{creation}] {operation.GetType()}";
		}

		lastOperationCount = operations.Length;
	}

	void DrawMain(double progress, Operation operation, TimeSpan time)
	{
		if (!ImGuiCustom.BeginProperties("Main")) return;

		ImGuiCustom.Property("Progress", progress.ToStringPercentage());
		ImGuiCustom.Property("Completed", operation.IsCompleted.ToString());
		ImGuiCustom.Property("Creation Time", operation.creationTime.ToStringDefault());
		ImGuiCustom.Property("Total Workload", operation.totalProcedureCount.ToStringDefault());

		ImGui.NewLine();

		ImGuiCustom.Property("Time Spent", time.ToStringDefault());
		ImGuiCustom.Property("Time Spend (All Worker)", operation.TotalTime.ToStringDefault());

		if (progress.AlmostEquals() || progress.AlmostEquals(1d))
		{
			ImGuiCustom.Property("Estimated Time Remain", "Unavailable");
			ImGuiCustom.Property("Estimated Completion Time", "Unavailable");
		}
		else
		{
			TimeSpan timeRemain = time / progress - time;
			DateTime timeFinish = DateTime.Now + timeRemain;

			ImGuiCustom.Property("Estimated Time Remain", timeRemain.ToStringDefault());
			ImGuiCustom.Property("Estimated Completion Time", timeFinish.ToStringDefault());
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
			ImGuiCustom.TableItem(count.ToStringDefault());

			if (divideByZero)
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

			ImGuiCustom.TableItem(guid.ToStringShort());
			ImGuiCustom.TableItem(time.ToStringDefault());
			ImGuiCustom.TableItem(procedure.index.ToStringDefault());
			ImGuiCustom.TableItem(procedure.Progress.ToStringPercentage());
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