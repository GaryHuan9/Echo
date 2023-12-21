using System;
using System.Diagnostics;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Memory;
using Echo.Core.InOut;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;
using OpenImageDenoisePrecompiled;

namespace Echo.UserInterface.Core.Areas;

public sealed class SystemUI : AreaUI
{
	public SystemUI(EchoUI root) : base(root) { }

	/// <summary>
	/// The main <see cref="Device"/> for this <see cref="EchoUI"/> instance.
	/// </summary>
	/// <remarks>This property is never null, however the instance might change.</remarks>
	public Device Device { get; private set; } = new();

	TimeSpan lastUpdateTime;
	string frameRateString;
	string utilizationString;
	ulong lastFrameCount;
	TimeSpan lastCpuTime;

	readonly WorkersReport workersReport = new();
	readonly Process currentProcess = Process.GetCurrentProcess();

	protected override string Name => "System";

	int frameFrequency;

	int FrameFrequency
	{
		get => frameFrequency;
		set
		{
			if (frameFrequency == value) return;
			root.FrameDelay = TimeSpan.FromSeconds(1f / value);
			frameFrequency = value;
		}
	}

	public override void Initialize()
	{
		base.Initialize();
		FrameFrequency = 60;
		OidnPrecompiled.TryLoad();
	}

	protected override void NewFrameWindow()
	{
		DrawGeneral();
		DrawGarbageCollector();
		DrawDeviceAndWorkers();
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing) Device.Dispose();
	}

	void DrawGeneral()
	{
		if (!ImGuiCustom.BeginSection("General")) return;

		if (ImGuiCustom.BeginProperties())
		{
			ImGuiCustom.Property("Operating System", Environment.OSVersion.VersionString);
			ImGuiCustom.Property("Debugger", Debugger.IsAttached ? "Present" : "Not Attached");
			ImGuiCustom.Property("Configuration", GetConfiguration());

			ImGuiCustom.PropertySeparator();

			UpdateFrameRateAndUtilization(root.Moment.elapsed - lastUpdateTime);

			ImGuiCustom.Property("Interface Frame Rate", frameRateString);
			ImGuiCustom.Property("Device Utilization", utilizationString);

			ImGuiCustom.EndProperties();
		}

		int frequency = FrameFrequency;
		ImGui.SliderInt("Update Frequency", ref frequency, 1, 120);
		FrameFrequency = frequency;

		if (ImGui.Button("Save Layout Configuration"))
		{
			string path = EchoUI.ConfigurationFilePath;
			ImGui.SaveIniSettingsToDisk(path);
			LogList.Add($"Configuration file saved to '{path}'.");
		}

		ImGuiCustom.EndSection();

		static string GetConfiguration()
		{
#if DEBUG
			return "DEBUG";
#elif RELEASE
			return "RELEASE";
#else
			return "UNKNOWN";
#endif
		}
	}

	void UpdateFrameRateAndUtilization(TimeSpan elapsed)
	{
		const float UpdatePeriod = 0.8f;

		if (elapsed < TimeSpan.FromSeconds(UpdatePeriod)) return;

		ulong frameCount = root.Moment.frame;
		TimeSpan cpuTime = currentProcess.UserProcessorTime;

		float elapsedSeconds = (float)elapsed.TotalSeconds;
		ulong elapsedFrames = frameCount - lastFrameCount;
		float elapsedCpuTime = (float)(cpuTime - lastCpuTime).TotalSeconds;

		frameRateString = (elapsedFrames / elapsedSeconds).ToInvariant();
		utilizationString = (elapsedCpuTime / (elapsedSeconds * Environment.ProcessorCount)).ToInvariantPercent();

		lastFrameCount = frameCount;
		lastCpuTime = cpuTime;
		lastUpdateTime += elapsed;
	}

	void DrawGarbageCollector()
	{
		if (!ImGuiCustom.BeginSection("Garbage Collector")) return;

		var info = GC.GetGCMemoryInfo();
		if (info.Index == 0) return;

		if (ImGui.Button("Collect All Generations"))
		{
			GC.Collect();
			LogList.Add("Triggered garbage collection on all generations.");
		}

		//Main table
		if (ImGuiCustom.BeginProperties())
		{
			ImGuiCustom.Property("GC Compacted", info.Compacted.ToString());
			ImGuiCustom.Property("GC Concurrent", info.Concurrent.ToString());
			ImGuiCustom.Property("GC Generation", info.Generation.ToString());

			ImGuiCustom.PropertySeparator();

			ImGuiCustom.Property("Mapped Memory", ((ulong)Environment.WorkingSet).ToInvariantData());
			ImGuiCustom.Property("Heap Size", ((ulong)info.HeapSizeBytes).ToInvariantData());
			ImGuiCustom.Property("Available Memory", ((ulong)info.TotalAvailableMemoryBytes).ToInvariantData());
			ImGuiCustom.Property("Pinned Object Count", info.PinnedObjectsCount.ToInvariant());
			ImGuiCustom.Property("Promoted Memory", ((ulong)info.PromotedBytes).ToInvariantData());
			ImGuiCustom.Property("GC Block Percentage", info.PauseTimePercentage.ToInvariantPercent());
			ImGuiCustom.Property("GC Fragmentation", ((ulong)info.FragmentedBytes).ToInvariantData());

			ImGuiCustom.EndProperties();
		}

		//Generations table
		if (ImGui.BeginTable("Generations", 5, ImGuiCustom.DefaultTableFlags))
		{
			var generations = info.GenerationInfo;

			ImGui.TableSetupColumn("Generation");
			ImGui.TableSetupColumn("Size Before");
			ImGui.TableSetupColumn("Size After");
			ImGui.TableSetupColumn("Frag. Before");
			ImGui.TableSetupColumn("Frag. After");
			ImGui.TableHeadersRow();

			for (int i = 0; i < generations.Length; i++)
			{
				ref readonly GCGenerationInfo generation = ref generations[i];

				ImGuiCustom.TableItem((i + 1).ToString());
				ImGuiCustom.TableItem(((ulong)generation.SizeBeforeBytes).ToInvariantData());
				ImGuiCustom.TableItem(((ulong)generation.SizeAfterBytes).ToInvariantData());
				ImGuiCustom.TableItem(((ulong)generation.FragmentationBeforeBytes).ToInvariantData());
				ImGuiCustom.TableItem(((ulong)generation.FragmentationAfterBytes).ToInvariantData());
			}

			ImGui.EndTable();
		}

		ImGuiCustom.EndSection();
	}

	void DrawDeviceAndWorkers()
	{
		if (!ImGuiCustom.BeginSection("Device, Workers, and Operations")) return;

		//Buttons
		ImGui.BeginDisabled(!Device.IsDispatched);

		if (ImGui.Button("Pause"))
		{
			Device.Pause();
			LogList.Add("Pausing dispatched operation on compute device.");
		}

		ImGui.SameLine();
		if (ImGui.Button("Resume"))
		{
			Device.Resume();
			LogList.Add("Resuming dispatched operation on compute device.");
		}

		ImGui.SameLine();
		if (ImGui.Button("Abort"))
		{
			Device.Abort();
			LogList.Add("Aborting dispatched operation on compute device.");
		}

		ImGui.EndDisabled();
		ImGui.SameLine();

		if (ImGui.Button("Recreate"))
		{
			ActionQueue.Enqueue("Device Dispose", Device.Dispose);
			Device = new Device();
		}

		//Status
		var operations = Device.Operations;
		int operationCount = operations.Count;

		if (ImGuiCustom.BeginProperties())
		{
			ImGuiCustom.Property("State", Device.IsDispatched ? "Running" : "Idle");
			ImGuiCustom.Property("Population", Device.Population.ToInvariant());
			ImGuiCustom.Property("Past Operation Count", operationCount.ToInvariant());

			ImGuiCustom.EndProperties();
		}

		//Workers
		Operation current = operations.Current;
		if (!Device.IsDispatched) current = null;

		if (ImGui.BeginTable("Workers Table", current == null ? 2 : 5, ImGuiCustom.DefaultTableFlags))
		{
			ImGui.TableSetupColumn("Worker Guid");
			ImGui.TableSetupColumn("State");

			if (current != null)
			{
				workersReport.Update(current);
				ImGui.TableSetupColumn("Time Active");
				ImGui.TableSetupColumn("Index");
				ImGui.TableSetupColumn("Progress");
			}

			ImGui.TableHeadersRow();

			foreach (IWorker worker in Device.Workers)
			{
				ImGuiCustom.TableItem(worker.Guid.ToInvariantShort());
				ImGuiCustom.TableItem(worker.State.ToDisplayString());

				if (current != null)
				{
					(TimeSpan activeTime, Procedure procedure) = workersReport[worker.Index];

					ImGuiCustom.TableItem(activeTime.ToInvariant());
					ImGuiCustom.TableItem(procedure.index.ToInvariant());
					ImGuiCustom.TableItem(procedure.Progress.ToInvariantPercent());
				}
			}

			ImGui.EndTable();
		}

		//Operations
		if (current != null && ImGui.BeginTable("Operations Table", 4, ImGuiCustom.DefaultTableFlags))
		{
			ImGui.TableSetupColumn("Operation Type");
			ImGui.TableSetupColumn("Dispatch Time");
			ImGui.TableSetupColumn("Status");
			ImGui.TableSetupColumn("Action");
			ImGui.TableHeadersRow();

			for (int i = operations.CurrentIndex; i < operationCount; i++)
			{
				Operation operation = operations[i];
				bool aborted = operations.Aborted(i);

				ImGuiCustom.TableItem(operation.GetType().Name);
				ImGuiCustom.TableItem(operation.creationTime.ToInvariant());
				ImGuiCustom.TableItem(operation == current ? "Executing" : aborted ? "Aborted" : "Awaiting");

				ImGui.TableNextColumn();
				ImGui.PushID(i);
				ImGui.BeginDisabled(aborted);
				if (ImGui.SmallButton("Abort")) Device.Abort(operation);
				ImGui.PopID();
				ImGui.EndDisabled();
			}

			ImGui.EndTable();
		}

		ImGuiCustom.EndSection();
	}

	class WorkersReport
	{
		int capacity;
		TimeSpan[] activeTimes = Array.Empty<TimeSpan>();
		Procedure[] procedures = Array.Empty<Procedure>();

		public (TimeSpan time, Procedure procedure) this[int index] => (activeTimes[index], procedures[index]);

		public void Update(Operation operation)
		{
			EnsureCapacity(operation.WorkerCount);

			var activeTimesFill = activeTimes.AsFill();
			var proceduresFill = procedures.AsFill();

			operation.FillWorkerTimes(ref activeTimesFill);
			operation.FillWorkerProcedures(ref proceduresFill);
		}

		void EnsureCapacity(int value)
		{
			if (value < capacity) return;

			capacity = Math.Max(capacity, 16);
			while (capacity < value) capacity *= 2;

			activeTimes = new TimeSpan[capacity];
			procedures = new Procedure[capacity];
		}
	}
}