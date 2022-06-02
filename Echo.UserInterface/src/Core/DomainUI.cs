using System;
using System.Globalization;
using System.Threading.Tasks;
using CodeHelpers.Packed;
using Echo.Common.Compute;
using Echo.Core.Evaluation.Distributions.Continuous;
using Echo.Core.Evaluation.Evaluators;
using Echo.Core.Evaluation.Operations;
using Echo.Core.Scenic.Examples;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Grid;
using ImGuiNET;

namespace Echo.UserInterface.Core;

public class DomainUI : AreaUI
{
	public DomainUI() : base("Domain") { }

	Device device;

	bool HasDevice => device is { Disposed: false };

	protected override void Draw()
	{
		if (ImGui.CollapsingHeader("System"))
		{
			ImGui.Text(Environment.OSVersion.VersionString);
			DrawGarbageCollector();
		}

		if (ImGui.CollapsingHeader("Device"))
		{
			if (!HasDevice)
			{
				if (ImGui.Button("Create")) device = Device.Create();
				ImGui.TextWrapped("Create a compute device to begin");
			}
			else DrawDevice();
		}

		if (ImGui.CollapsingHeader("Workers"))
		{
			if (HasDevice) DrawWorkers();
			else ImGui.TextWrapped("Create a compute device to begin");
		}
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing) device?.Dispose();
	}

	void DrawGarbageCollector()
	{
		var info = GC.GetGCMemoryInfo();

		if (ImGui.Button("GC Collect All")) GC.Collect();

		//Main table
		if (ImGui.BeginTable("GC Main", 2, ImGuiTableFlags.BordersOuter))
		{
			Row("GC Compacted", info.Compacted.ToString());
			Row("GC Concurrent", info.Concurrent.ToString());
			Row("GC Generation", info.Generation.ToString());

			Row("Mapped Memory", ((ulong)Environment.WorkingSet).ToStringData());
			Row("Heap Size", ((ulong)info.HeapSizeBytes).ToStringData());
			Row("Available Memory", ((ulong)info.TotalAvailableMemoryBytes).ToStringData());
			Row("Pinned Object Count", info.PinnedObjectsCount.ToString());
			Row("Promoted Memory", ((ulong)info.PromotedBytes).ToStringData());
			Row("GC Block Percentage", info.PauseTimePercentage.ToStringPercentage());
			Row("GC Fragmentation", ((ulong)info.FragmentedBytes).ToStringData());

			ImGui.EndTable();

			static void Row(string label, string value)
			{
				TableItem(label);
				TableItem(value);
			}
		}

		//Table for generations
		if (ImGui.BeginTable("GC Generations", 5, ImGuiTableFlags.BordersOuter))
		{
			var generations = info.GenerationInfo;

			ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
			TableItem("Generation");
			TableItem("Size Before", true);
			TableItem("Size After", true);
			TableItem("Frag. Before", true);
			TableItem("Frag. After", true);

			for (int i = 0; i < generations.Length; i++)
			{
				ref readonly GCGenerationInfo generation = ref generations[i];

				TableItem((i + 1).ToString());
				TableItem(((ulong)generation.SizeBeforeBytes).ToStringData());
				TableItem(((ulong)generation.SizeAfterBytes).ToStringData());
				TableItem(((ulong)generation.FragmentationBeforeBytes).ToStringData());
				TableItem(((ulong)generation.FragmentationAfterBytes).ToStringData());
			}

			ImGui.EndTable();
		}
	}

	void DrawDevice()
	{
		//Buttons
		bool idle = device.IsIdle;
		ImGui.BeginDisabled(!idle);

		if (ImGui.Button("Dispatch")) DispatchToDevice(device);

		ImGui.EndDisabled();
		ImGui.BeginDisabled(idle);

		ImGui.SameLine();
		if (ImGui.Button("Pause")) device.Pause();

		ImGui.SameLine();
		if (ImGui.Button("Resume")) device.Resume();

		ImGui.EndDisabled();

		ImGui.SameLine();
		if (ImGui.Button("Dispose"))
		{
			device.Dispose();
			device = null;
			return;
		}

		//Status
		ImGui.TextUnformatted($"CPU compute device {(device.IsIdle ? "idling" : "running")} with {device.Population} workers");
		ImGui.TextUnformatted($"Progress {device.StartedProgress:P2}");
		ImGui.TextUnformatted($"Total Time {device.StartedTotalTime:hh\\:mm\\:ss}");
		ImGui.TextUnformatted($"Time {device.StartedTime:hh\\:mm\\:ss}");
	}

	void DrawWorkers()
	{
		//Worker table
		if (ImGui.BeginTable("State", 3, ImGuiTableFlags.BordersOuter))
		{
			ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
			TableItem("Worker");
			TableItem("State");
			TableItem("Progress");

			foreach (IWorker worker in device.Workers)
			{
				TableItem(worker.DisplayLabel);
				TableItem(worker.State.ToDisplayString());
				TableItem(worker.Progress.ToStringPercentage());
			}

			ImGui.EndTable();
		}
	}

	static void TableItem(string value, bool wrap = false)
	{
		ImGui.TableNextColumn();
		if (wrap) ImGui.TextWrapped(value);
		else ImGui.TextUnformatted(value);
	}

	static void DispatchToDevice(Device device)
	{
		var scene = new SingleBunny();

		var prepareProfile = new ScenePrepareProfile();

		var evaluationProfile = new TiledEvaluationProfile
		{
			Scene = new PreparedScene(scene, prepareProfile),
			Evaluator = new PathTracedEvaluator(),
			Distribution = new StratifiedDistribution { Extend = 64 },
			Buffer = new RenderBuffer(new Int2(960, 540)),
			MinEpoch = 1,
			MaxEpoch = 1
		};

		var operation = new TiledEvaluationOperation
		{
			Profile = evaluationProfile
		};

		device.Dispatch(operation);
	}
}