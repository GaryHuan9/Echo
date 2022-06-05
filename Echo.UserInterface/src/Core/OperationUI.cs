using System;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Common.Compute;
using Echo.Common.Compute.Statistics;
using Echo.Common.Memory;
using ImGuiNET;

namespace Echo.UserInterface.Core;

public class OperationUI : AreaUI
{
	public OperationUI() : base("Operation") { }

	EventRow[] eventRows = Array.Empty<EventRow>();
	readonly WorkerData workerData = new();

	protected override void Draw()
	{
		// ImGui.ListBox("Select", )

		var device = Device.Instance;
		var operation = device?.LatestOperation;
		if (operation == null) return;

		double progress = operation.Progress;
		TimeSpan time = operation.Time;

		//Draw properties
		DrawMain(progress, operation, time);
		DrawEventRows(operation, time, progress);
		DrawWorkers(operation);
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

		if (progress.AlmostEquals(1d))
		{
			DateTime completionTime = operation.creationTime + time;

			ImGuiCustom.Property("Time Remain", TimeSpan.Zero.ToStringDefault());
			ImGuiCustom.Property("Completion Time", completionTime.ToStringDefault());
		}
		else if (progress.AlmostEquals())
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
		if (operation.EventRowCount <= 0 || !ImGui.BeginTable("Events", 4, ImGuiTableFlags.BordersOuter)) return;

		//Gather data
		double timeR = 1d / time.TotalSeconds;
		double progressR = 1d / progress;
		bool divideByZero = time == TimeSpan.Zero || progress.AlmostEquals();

		if (eventRows.Length < operation.EventRowCount) eventRows = new EventRow[operation.EventRowCount];

		SpanFill<EventRow> fill = eventRows.AsFill();
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
		if (operation.WorkerCount <= 0 || !ImGui.BeginTable("Workers", 4, ImGuiTableFlags.BordersOuter)) return;

		//Gather data
		workerData.EnsureCapacity(operation.WorkerCount);
		workerData.FillAll(operation);

		//Draw
		ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
		ImGuiCustom.TableItem("Guid");
		ImGuiCustom.TableItem("Time Spent");
		ImGuiCustom.TableItem("Procedure Index");
		ImGuiCustom.TableItem("Procedure Progress");

		for (int i = 0; i < workerData.Length; i++)
		{
			(Guid guid, TimeSpan time, Procedure procedure) = workerData[i];

			ImGuiCustom.TableItem(guid.ToString("D"));
			ImGuiCustom.TableItem(time.ToStringDefault());
			ImGuiCustom.TableItem(procedure.index.ToStringDefault());
			ImGuiCustom.TableItem(procedure.Progress.ToStringDefault());
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