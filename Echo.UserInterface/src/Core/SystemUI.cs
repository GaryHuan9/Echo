using System;
using System.Globalization;
using System.Threading.Tasks;
using CodeHelpers.Mathematics;
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

public class SystemUI : AreaUI
{
	public SystemUI() : base("System") { }

	Device device;

	bool HasDevice => device is { Disposed: false };

	protected override void Draw()
	{
		ImGui.Text(Environment.OSVersion.VersionString);

		if (ImGui.CollapsingHeader("Garbage Collector")) DrawGarbageCollector();

		if (ImGui.CollapsingHeader("Device and Workers"))
		{
			if (!HasDevice)
			{
				if (ImGui.Button("Create")) device = Device.Create();
				ImGui.TextWrapped("Create a compute device to begin");
			}
			else DrawDevice();

			if (HasDevice) DrawWorkers();
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

		if (ImGui.Button("Collect All")) GC.Collect();

		//Main table
		if (ImGuiCustom.BeginProperties("Main"))
		{
			ImGuiCustom.Property("GC Compacted", info.Compacted.ToString());
			ImGuiCustom.Property("GC Concurrent", info.Concurrent.ToString());
			ImGuiCustom.Property("GC Generation", info.Generation.ToString());

			ImGuiCustom.Property("Mapped Memory", ((ulong)Environment.WorkingSet).ToStringData());
			ImGuiCustom.Property("Heap Size", ((ulong)info.HeapSizeBytes).ToStringData());
			ImGuiCustom.Property("Available Memory", ((ulong)info.TotalAvailableMemoryBytes).ToStringData());
			ImGuiCustom.Property("Pinned Object Count", info.PinnedObjectsCount.ToString());
			ImGuiCustom.Property("Promoted Memory", ((ulong)info.PromotedBytes).ToStringData());
			ImGuiCustom.Property("GC Block Percentage", info.PauseTimePercentage.ToStringPercentage());
			ImGuiCustom.Property("GC Fragmentation", ((ulong)info.FragmentedBytes).ToStringData());

			ImGuiCustom.EndProperties();
		}

		//Generations table
		if (ImGui.BeginTable("Generations", 5, ImGuiTableFlags.BordersOuter))
		{
			var generations = info.GenerationInfo;

			ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
			ImGuiCustom.TableItem("Generation");
			ImGuiCustom.TableItem("Size Before", true);
			ImGuiCustom.TableItem("Size After", true);
			ImGuiCustom.TableItem("Frag. Before", true);
			ImGuiCustom.TableItem("Frag. After", true);

			for (int i = 0; i < generations.Length; i++)
			{
				ref readonly GCGenerationInfo generation = ref generations[i];

				ImGuiCustom.TableItem((i + 1).ToString());
				ImGuiCustom.TableItem(((ulong)generation.SizeBeforeBytes).ToStringData());
				ImGuiCustom.TableItem(((ulong)generation.SizeAfterBytes).ToStringData());
				ImGuiCustom.TableItem(((ulong)generation.FragmentationBeforeBytes).ToStringData());
				ImGuiCustom.TableItem(((ulong)generation.FragmentationAfterBytes).ToStringData());
			}

			ImGui.EndTable();
		}
	}

	void DrawDevice()
	{
		//Buttons
		bool idle = device.IsIdle;
		ImGui.BeginDisabled(!idle);

		if (ImGui.Button("Dispatch")) DispatchDevice(device);

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
			DisposeDevice(device);
			device = null;
			return;
		}

		//Status
		if (ImGuiCustom.BeginProperties("Main"))
		{
			double progress = device.StartedProgress;
			TimeSpan time = device.StartedTime;

			ImGuiCustom.Property("State", device.IsIdle ? "Idle" : "Running");
			ImGuiCustom.Property("Population", device.Population.ToStringDefault());
			ImGuiCustom.Property("Progress", progress.ToStringPercentage());

			ImGui.NewLine();

			ImGuiCustom.Property("Time Spent", time.ToStringDefault());
			ImGuiCustom.Property("Time Spend (All Worker)", device.StartedTotalTime.ToStringDefault());

			if (progress.AlmostEquals())
			{
				ImGuiCustom.Property("Time Remain", "Unavailable");
				ImGuiCustom.Property("Completion Time", "Unavailable");
			}
			else
			{
				TimeSpan timeRemain = time / progress - time;
				DateTime timeFinish = DateTime.Now + timeRemain;

				ImGuiCustom.Property("Time Remain", timeRemain.ToStringDefault());
				ImGuiCustom.Property("Completion Time", timeFinish.ToStringDefault());
			}

			ImGuiCustom.EndProperties();
		}
	}

	void DrawWorkers()
	{
		//Worker table
		if (ImGui.BeginTable("State", 4, ImGuiTableFlags.BordersOuter))
		{
			ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
			ImGuiCustom.TableItem("Worker");
			ImGuiCustom.TableItem("State");
			ImGuiCustom.TableItem("Progress");
			ImGuiCustom.TableItem("Thread Id");

			foreach (IWorker worker in device.Workers)
			{
				ImGuiCustom.TableItem(worker.DisplayLabel);
				ImGuiCustom.TableItem(worker.State.ToDisplayString());
				ImGuiCustom.TableItem(worker.Progress.ToStringPercentage());

				int? id = worker.ThreadId;
				ImGuiCustom.TableItem(id == null ? "Undetermined" : id.Value.ToStringDefault());
			}

			ImGui.EndTable();
		}
	}

	static void DispatchDevice(Device device)
	{
		ActionQueue.Enqueue(Dispatch, "Device Dispatch");

		void Dispatch()
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

	static void DisposeDevice(Device device) => ActionQueue.Enqueue(device.Dispose, "Device Dispose");
}