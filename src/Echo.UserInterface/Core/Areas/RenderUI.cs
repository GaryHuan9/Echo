using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Common;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Compute.Statistics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.InOut;
using Echo.Core.Processes;
using Echo.Core.Processes.Composition;
using Echo.Core.Processes.Evaluation;
using Echo.Core.Processes.Preparation;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Evaluation;
using Echo.Core.Textures.Grids;
using Echo.UserInterface.Core.Common;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public sealed class RenderUI : AreaUI
{
	public RenderUI(EchoUI root) : base(root) { }

	readonly List<ScheduledRender> renders = new();
	readonly List<string> renderStrings = new();

	int currentIndex;
	bool trackLatest = true;
	Operation lastTracked;
	EventRow[] eventRows;

	ViewerUI viewerUI;
	FileUI dialogueUI;

	protected override string Name => "Render";

	public override void Initialize()
	{
		base.Initialize();
		viewerUI = root.Find<ViewerUI>();
		dialogueUI = root.Find<FileUI>();
	}

	public void AddRender(ScheduledRender render)
	{
		renders.Add(render);
		renderStrings.Add($"Scheduled Render - {render.preparationOperation.creationTime.ToInvariant()}");
	}

	public void ClearRenders()
	{
		renders.Clear();
		renderStrings.Clear();
	}

	protected override void NewFrameWindow()
	{
		if (renders.Count == 0)
		{
			ImGui.TextUnformatted("No scheduled render.");
			return;
		}

		if (ImGuiCustom.Selector("Select", CollectionsMarshal.AsSpan(renderStrings), ref currentIndex)) trackLatest = false;
		ImGui.Checkbox("Track Latest Operation", ref trackLatest);

		if (trackLatest)
		{
			while (currentIndex < renders.Count - 1 && renders[currentIndex].IsCompleted) ++currentIndex;
			while (currentIndex > 0 && !renders[currentIndex - 1].IsCompleted) --currentIndex;
		}

		ScheduledRender render = renders[currentIndex];

		if (!render.IsCompleted)
		{
			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();
			if (ImGui.Button("Abort Render")) render.Abort();
		}

		ImGui.Separator();
		DrawRenderOverview(render);
		Operation tracking = null;

		if (trackLatest)
		{
			if (render.IsCompleted)
			{
				//Just finished the final operation in this render, display the result
				if (lastTracked != null) viewerUI.Track(render.texture);
			}
			else tracking = render.operations[render.CurrentIndex];
		}

		if (ImGui.BeginTabBar("Operations", ImGuiTabBarFlags.TabListPopupButton))
		{
			for (int i = 0; i < render.operations.Length; i++)
			{
				ImGui.PushID(i);

				Operation operation = render.operations[i];
				bool track = tracking == operation;
				bool clicked = DrawOperation(operation, track) && !track;
				if (clicked && tracking != null) trackLatest = false;

				ImGui.PopID();
			}

			ImGui.EndTabBar();
		}

		lastTracked = tracking;
	}

	void DrawRenderOverview(ScheduledRender render)
	{
		if (ImGuiCustom.BeginSection("Overview"))
		{
			if (ImGuiCustom.BeginProperties())
			{
				ImGuiCustom.Property("Completed", render.IsCompleted.ToString());
				ImGuiCustom.Property("Creation Time", render.preparationOperation.creationTime.ToInvariant());
				ImGuiCustom.Property("Operations", render.operations.Length.ToInvariant());
				ImGuiCustom.Property("Current Operation", render.CurrentIndex.ToInvariant());

				ImGuiCustom.PropertySeparator();

				RenderProfile profile = render.profile;
				ImGuiCustom.Property("Total Evaluations", profile.EvaluationProfiles.Length.ToInvariant());
				ImGuiCustom.Property("Total Compositions", profile.CompositionLayers.Length.ToInvariant());

				ImGuiCustom.EndProperties();
			}

			ImGuiCustom.EndSection();
		}

		if (ImGuiCustom.BeginSection("Destination"))
		{
			RenderTexture texture = render.texture;

			if (ImGuiCustom.BeginProperties())
			{
				ImGuiCustom.Property("Resolution", texture.size.ToInvariant());
				ImGuiCustom.Property("Tile Size", texture.tileSize.ToInvariant());

				ImGuiCustom.EndProperties();
			}

			TextureGrid<NormalDepth128> depthTexture = null;
			if (render.IsCompleted && !render.texture.TryGetLayer(out depthTexture)) depthTexture = null;

			if (ImGui.BeginTable("Layers", 4, ImGuiCustom.DefaultTableFlags))
			{
				ImGui.TableSetupColumn("Name");
				ImGui.TableSetupColumn("Type");
				ImGui.TableSetupColumn("Color");
				ImGui.TableSetupColumn("Actions");
				ImGui.TableHeadersRow();

				foreach ((string name, TextureGrid layer) in texture.Layers)
				{
					ImGuiCustom.TableItem(name);
					Type type = layer.GetType();

					ImGuiCustom.TableItem(type.Name);
					ImGuiCustom.TableItem(type.IsGenericType ? type.GetGenericArguments()[0].Name : "");

					ImGui.PushID(name);
					ImGui.TableNextColumn();
					if (ImGui.SmallButton("View")) viewerUI.Track(layer);

					if (depthTexture != null)
					{
						ImGui.SameLine();
						if (ImGui.SmallButton("Explore")) viewerUI.Track(layer, depthTexture, render.Scene.camera);
					}

					ImGui.SameLine();
					if (ImGui.SmallButton("Save")) dialogueUI.Open($"{name}.png", false, path => SaveTexture(path, layer));
					ImGui.PopID();
				}

				ImGui.EndTable();
			}

			ImGuiCustom.EndSection();
		}
	}

	bool DrawOperation(Operation operation, bool track)
	{
		bool active;
		bool clicked;

		switch (operation)
		{
			case PreparationOperation casted:
			{
				active = BeginTabItem("Preparation");
				clicked = ImGui.IsItemClicked();
				if (active) DrawOperation(casted);
				break;
			}
			case EvaluationOperation casted:
			{
				if (track && lastTracked != operation) viewerUI.Track(casted);

				active = BeginTabItem("Evaluation");
				clicked = ImGui.IsItemClicked();
				if (active) DrawOperation(casted);
				break;
			}
			case CompositionOperation casted:
			{
				active = BeginTabItem("Composition");
				clicked = ImGui.IsItemClicked();
				if (active) DrawOperation(casted);
				break;
			}
			default: throw new ArgumentOutOfRangeException(nameof(operation));
		}

		if (active) ImGui.EndTabItem();
		return clicked;

		unsafe bool BeginTabItem(ReadOnlySpan<char> label)
		{
			ImGuiTabItemFlags flags = ImGuiTabItemFlags.None;
			if (track) flags |= ImGuiTabItemFlags.SetSelected;

			Span<byte> bytes = stackalloc byte[label.Length * 2 + 1];
			bytes[Encoding.UTF8.GetBytes(label, bytes)] = 0;

			fixed (byte* pointer = bytes) return ImGuiNative.igBeginTabItem(pointer, null, flags) > 0;
		}
	}

	void DrawOperation(PreparationOperation operation)
	{
		PreparedScene scene = operation.PreparedScene;

		if (scene == null)
		{
			ImGui.TextUnformatted("Awaiting for preparation.");
			return;
		}

		if (ImGuiCustom.BeginSection("Overview"))
		{
			if (ImGuiCustom.BeginProperties())
			{
				ImGuiCustom.Property("Total Infinite Light", scene.infiniteLights.Length.ToInvariant());
				ImGuiCustom.Property("Infinite Lights Power", scene.infiniteLightsPower.ToInvariant());
				ImGuiCustom.Property("All Lights Power", (scene.infiniteLightsPower + scene.lightPicker.Power).ToInvariant());

				ImGuiCustom.PropertySeparator();

				if (scene.camera != null)
				{
					ImGuiCustom.Property("Camera Position", scene.camera.ContainedPosition.ToInvariant());
					ImGuiCustom.Property("Camera Forward", (scene.camera.ContainedRotation * Float3.Forward).ToInvariant());
					ImGuiCustom.Property("Camera Up", (scene.camera.ContainedRotation * Float3.Up).ToInvariant());
					ImGuiCustom.Property("Camera Field of View", scene.camera.FieldOfView.ToInvariant());
				}
				else ImGui.TextUnformatted("No camera found");

				ImGuiCustom.EndProperties();
			}

			ImGuiCustom.EndSection();
		}

		PreparedPack pack = scene;

		if (ImGuiCustom.BeginSection("Geometries"))
		{
			if (ImGuiCustom.BeginProperties())
			{
				ImGuiCustom.Property("Enclosing Box", pack.accelerator.BoxBound.ToInvariant());
				ImGuiCustom.Property("Enclosing Sphere", pack.accelerator.SphereBound.ToInvariant());
				ImGuiCustom.Property("Accelerator", pack.accelerator.GetType().Name);

				ImGuiCustom.EndProperties();
			}

			if (ImGui.BeginTable("Table", 4, ImGuiCustom.DefaultTableFlags))
			{
				ImGui.TableSetupColumn("Kind");
				ImGui.TableSetupColumn("Triangle");
				ImGui.TableSetupColumn("Sphere");
				ImGui.TableSetupColumn("Instance");
				ImGui.TableHeadersRow();

				ImGuiCustom.TableItem("Unique");
				ImGuiCustom.TableItem(pack.geometries.counts.triangle.ToInvariant());
				ImGuiCustom.TableItem(pack.geometries.counts.sphere.ToInvariant());
				ImGuiCustom.TableItem(pack.geometries.counts.instance.ToInvariant());

				ImGuiCustom.TableItem("Total");
				ImGuiCustom.TableItem(pack.geometries.countsTotal.triangle.ToInvariant());
				ImGuiCustom.TableItem(pack.geometries.countsTotal.sphere.ToInvariant());
				ImGuiCustom.TableItem(pack.geometries.countsTotal.instance.ToInvariant());

				ImGui.EndTable();
			}

			ImGuiCustom.EndSection();
		}

		if (ImGuiCustom.BeginSection("Lights"))
		{
			if (ImGuiCustom.BeginProperties())
			{
				ImGuiCustom.Property("Total Power", pack.lightPicker.Power.ToInvariant());
				ImGuiCustom.Property("Enclosing Box", pack.lightPicker.BoxBound.ToInvariant());
				ImGuiCustom.Property("Enclosing Cone", pack.lightPicker.ConeBound.ToInvariant());

				ImGuiCustom.PropertySeparator();

				ImGuiCustom.Property("Total Point Light", pack.lights.points.Length.ToInvariant());
				ImGuiCustom.Property("Total Emissive Triangle", pack.lights.emissiveCounts.triangle.ToInvariant());
				ImGuiCustom.Property("Total Emissive Sphere", pack.lights.emissiveCounts.sphere.ToInvariant());
				ImGuiCustom.Property("Total Emissive Instances", pack.lights.emissiveCounts.instance.ToInvariant());

				ImGuiCustom.EndProperties();
			}

			ImGuiCustom.EndSection();
		}
	}

	void DrawOperation(EvaluationOperation operation)
	{
		double progress = operation.Progress;
		TimeSpan time = operation.Time;

		if (ImGuiCustom.BeginSection("Overview"))
		{
			if (BeginOperationOverviewProperties(operation))
			{
				if (progress is > 0f and < 1f)
				{
					TimeSpan timeRemain = time / progress - time;
					DateTime timeFinish = DateTime.Now + timeRemain;

					ImGuiCustom.Property("Estimated Time Remain", timeRemain.ToInvariant());
					ImGuiCustom.Property("Estimated Completion Time", timeFinish.ToInvariant());
				}

				if (progress > 0f)
				{
					int pixels = ((TextureGrid)operation.destination).size.Product;
					double average = operation.TotalSamples / (pixels * progress);
					ImGuiCustom.Property("Sample Per Pixel", average.ToInvariant());
				}

				ImGuiCustom.EndProperties();
			}

			ImGuiCustom.EndSection();
		}

		if (ImGuiCustom.BeginSection("Profile"))
		{
			if (ImGuiCustom.BeginProperties())
			{
				EvaluationProfile profile = operation.profile;

				ImGuiCustom.Property("Evaluator", profile.Evaluator.GetType().Name);
				ImGuiCustom.Property("Distribution", profile.Distribution.GetType().Name);
				ImGuiCustom.Property("Destination", profile.LayerName);

				ImGui.SameLine();
				ImGui.Spacing();
				ImGui.SameLine();
				if (ImGui.SmallButton("View Evaluation")) viewerUI.Track(operation);

				ImGuiCustom.PropertySeparator();

				ImGuiCustom.Property("Epoch Size", profile.Distribution.Extend.ToInvariant());
				ImGuiCustom.Property("Min Epoch Count", profile.MinEpoch.ToInvariant());
				ImGuiCustom.Property("Max Epoch Count", profile.MaxEpoch.ToInvariant());
				ImGuiCustom.Property("Noise Threshold", profile.NoiseThreshold.ToString("R", InvariantFormat.Culture));

				ImGuiCustom.EndProperties();
			}

			ImGuiCustom.EndSection();
		}

		if (EvaluationOperation.EventRowCount > 0 && ImGuiCustom.BeginSection("Events"))
		{
			double timeR = 1d / time.TotalSeconds;
			double remaining = 1d / progress - 1d;
			bool printRate = time > TimeSpan.Zero;
			bool printRemaining = FastMath.Positive((float)progress) && progress < 1d;

			if (ImGui.BeginTable("Table", 2 + (printRate ? 1 : 0) + (printRemaining ? 1 : 0), ImGuiCustom.DefaultTableFlags))
			{
				Utility.EnsureCapacity(ref eventRows, EvaluationOperation.EventRowCount);

				SpanFill<EventRow> fill = eventRows;
				operation.FillEventRows(ref fill);

				ImGui.TableSetupColumn("Label");
				ImGui.TableSetupColumn("Total Done");
				if (printRate) ImGui.TableSetupColumn("Per Second");
				if (printRemaining) ImGui.TableSetupColumn("Remaining");
				ImGui.TableHeadersRow();

				foreach ((string label, ulong count) in fill.Filled)
				{
					ImGuiCustom.TableItem(label);
					ImGuiCustom.TableItem(count.ToInvariant());

					if (printRate) ImGuiCustom.TableItem(((float)(count * timeR)).ToInvariant());
					if (printRemaining) ImGuiCustom.TableItem(((ulong)(count * remaining)).ToInvariant());
				}

				ImGui.EndTable();
			}

			ImGuiCustom.EndSection();
		}
	}

	void DrawOperation(CompositionOperation operation)
	{
		if (ImGuiCustom.BeginSection("Overview"))
		{
			if (BeginOperationOverviewProperties(operation)) ImGuiCustom.EndProperties();

			ImGuiCustom.EndSection();
		}

		if (ImGuiCustom.BeginSection("Layers"))
		{
			if (ImGui.BeginTable("Table", 3, ImGuiCustom.DefaultTableFlags))
			{
				ImGui.TableSetupColumn("Order");
				ImGui.TableSetupColumn("Name");
				ImGui.TableSetupColumn("Status");
				ImGui.TableHeadersRow();

				for (int i = 0; i < operation.layers.Length; i++)
				{
					ImGuiCustom.TableItem(i.ToInvariant());
					ImGuiCustom.TableItem(operation.layers[i].GetType().Name);

					if (i >= operation.CompletedLayerCount) ImGuiCustom.TableItem("Awaiting");
					else ImGuiCustom.TableItem(operation.ErrorMessages[i] ?? "Done", true);
				}

				ImGui.EndTable();
			}

			ImGuiCustom.EndSection();
		}
	}

	static void SaveTexture(string path, TextureGrid texture) => ActionQueue.Enqueue("Save Texture to Disk", () => texture.Save(path));

	static bool BeginOperationOverviewProperties(Operation operation)
	{
		float width = ImGui.GetContentRegionAvail().X;
		ImGui.ProgressBar((float)operation.Progress, new Vector2(width, 0f));

		if (!ImGuiCustom.BeginProperties()) return false;

		ImGuiCustom.Property("Completed", operation.IsCompleted.ToString());
		ImGuiCustom.Property("Total Workload", operation.TotalProcedureCount.ToInvariant());
		ImGuiCustom.Property("Completed Work", operation.CompletedProcedureCount.ToInvariant());

		ImGuiCustom.PropertySeparator();

		ImGuiCustom.Property("Real Time Spent", operation.Time.ToInvariant());
		ImGuiCustom.Property("Worker Time Spent", operation.TotalTime.ToInvariant());

		return true;
	}
}