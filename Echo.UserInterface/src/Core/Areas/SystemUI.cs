using System;
using System.Diagnostics;
using Echo.Core.Common.Compute;
using Echo.Core.InOut;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public sealed class SystemUI : AreaUI
{
	public SystemUI(EchoUI root) : base(root) { }

	/// <summary>
	/// The main <see cref="Device"/> for this <see cref="EchoUI"/> instance.
	/// </summary>
	/// <remarks>This property is never null, however the instance might change.</remarks>
	public Device Device { get; private set; } = Device.Create();

	string frameRate;
	TimeSpan lastUpdateTime;
	int updateFrequency = 100;

	protected override string Name => "System";

	public override void Initialize()
	{
		base.Initialize();
		AssignUpdateFrequency();
	}

	protected override void Update(in Moment moment)
	{
		ImGui.SetNextItemOpen(true, ImGuiCond.Once);
		if (ImGui.CollapsingHeader("General")) DrawGeneral(moment);

		ImGui.SetNextItemOpen(true, ImGuiCond.Once);
		if (ImGui.CollapsingHeader("Garbage Collector")) DrawGarbageCollector();

		ImGui.SetNextItemOpen(true, ImGuiCond.Once);
		if (ImGui.CollapsingHeader("Device and Workers"))
		{
			DrawDevice();
			DrawWorkers();
		}
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing) Device.Dispose();
	}

	void AssignUpdateFrequency() => root.UpdateDelay = TimeSpan.FromSeconds(1f / updateFrequency);

	void DrawGeneral(in Moment moment)
	{
		if (ImGuiCustom.BeginProperties("Main"))
		{
			ImGuiCustom.Property("Operating System", Environment.OSVersion.VersionString);
			ImGuiCustom.Property("Debugger", Debugger.IsAttached ? "Present" : "Not Attached");
			ImGuiCustom.Property("Compiler Mode", GetCompilerMode());

			ImGui.NewLine();

			if (moment.elapsed - lastUpdateTime > TimeSpan.FromSeconds(0.5f))
			{
				float rate = 1f / (float)moment.delta.TotalSeconds;
				frameRate = rate.ToInvariant();
				lastUpdateTime = moment.elapsed;
			}

			ImGuiCustom.Property("Frame Rate", frameRate);
			ImGuiCustom.EndProperties();
		}

		int oldUpdateFrequency = updateFrequency;
		ImGui.SliderInt("Refresh Frequency", ref updateFrequency, 1, 120);
		if (oldUpdateFrequency != updateFrequency) AssignUpdateFrequency();

		static string GetCompilerMode()
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

	void DrawGarbageCollector()
	{
		var info = GC.GetGCMemoryInfo();

		if (ImGui.Button("Collect All Generations"))
		{
			GC.Collect();
			LogList.Add("Triggered garbage collection on all generations.");
		}

		//Main table
		if (ImGuiCustom.BeginProperties("Main"))
		{
			ImGuiCustom.Property("GC Compacted", info.Compacted.ToString());
			ImGuiCustom.Property("GC Concurrent", info.Concurrent.ToString());
			ImGuiCustom.Property("GC Generation", info.Generation.ToString());

			ImGuiCustom.Property("Mapped Memory", ((ulong)Environment.WorkingSet).ToInvariantData());
			ImGuiCustom.Property("Heap Size", ((ulong)info.HeapSizeBytes).ToInvariantData());
			ImGuiCustom.Property("Available Memory", ((ulong)info.TotalAvailableMemoryBytes).ToInvariantData());
			ImGuiCustom.Property("Pinned Object Count", info.PinnedObjectsCount.ToString());
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
	}

	void DrawDevice()
	{
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
			Device = Device.Create();
		}

		//Status
		if (ImGuiCustom.BeginProperties("Main"))
		{
			ImGuiCustom.Property("State", Device.IsDispatched ? "Running" : "Idle");
			ImGuiCustom.Property("Population", Device.Population.ToInvariant());

			if (Device.Operations.Count > 0)
			{
				ImGuiCustom.Property("Latest Dispatch", Device.Operations[^1].creationTime.ToInvariant());
				ImGuiCustom.Property("Past Operation Count", Device.Operations.Count.ToInvariant());
			}
			else
			{
				ImGuiCustom.Property("Latest Dispatch", "None");
				ImGuiCustom.Property("Past Operation Count", "0");
			}

			ImGuiCustom.EndProperties();
		}
	}

	void DrawWorkers()
	{
		//Worker table
		if (ImGui.BeginTable("State", 3, ImGuiCustom.DefaultTableFlags))
		{
			ImGui.TableSetupColumn("Index");
			ImGui.TableSetupColumn("State");
			ImGui.TableSetupColumn("Guid");
			ImGui.TableHeadersRow();

			foreach (IWorker worker in Device.Workers)
			{
				ImGuiCustom.TableItem($"0x{worker.Index:X4}");
				ImGuiCustom.TableItem(worker.State.ToDisplayString());
				ImGuiCustom.TableItem(worker.Guid.ToInvariantShort());
			}

			ImGui.EndTable();
		}
	}


}