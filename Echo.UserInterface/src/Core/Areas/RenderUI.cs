using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Common;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Compute.Statistics;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.InOut;
using Echo.Core.Processes;
using Echo.Core.Processes.Composition;
using Echo.Core.Processes.Evaluation;
using Echo.Core.Processes.Preparation;
using Echo.Core.Textures.Evaluation;
using Echo.Core.Textures.Grids;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public sealed class RenderUI : AreaUI
{
	public RenderUI(EchoUI root) : base(root) { }

	readonly List<ScheduledRender> renders = new();
	readonly List<string> renderLabels = new();

	int currentIndex;
	bool trackLatest = true;
	EventRow[] eventRows;

	protected override string Name => "Render";

	public void AddRender(ScheduledRender render)
	{
		renders.Add(render);
		renderLabels.Add($"Scheduled Render - {render.preparationOperation.creationTime.ToInvariant()}");
	}

	public void ClearRenders()
	{
		renders.Clear();
		renderLabels.Clear();
	}

	protected override void Update(in Moment moment)
	{
		if (renders.Count == 0)
		{
			ImGui.TextUnformatted("No scheduled render.");
			return;
		}

		if (ImGuiCustom.Selector("Select", CollectionsMarshal.AsSpan(renderLabels), ref currentIndex)) trackLatest = false;
		ImGui.Checkbox("Track Latest", ref trackLatest);
		if (trackLatest) currentIndex = renders.Count - 1;

		ImGui.Separator();

		ScheduledRender render = renders[currentIndex];
		DrawRenderOverview(render);
		ImGui.NewLine();

		if (ImGui.BeginTabBar("Operations", ImGuiTabBarFlags.TabListPopupButton))
		{
			for (int i = 0; i < render.operations.Length; i++)
			{
				ImGui.PushID(i);

				bool select = i == Math.Min(render.CurrentIndex, render.operations.Length - 1);
				bool clicked = DrawOperation(render.operations[i], trackLatest && select);
				if (clicked && !select) trackLatest = false;

				ImGui.PopID();
			}

			ImGui.EndTabBar();
		}
	}

	void DrawRenderOverview(ScheduledRender render)
	{
		if (ImGuiCustom.OpenHeader("Overview") && ImGuiCustom.BeginProperties())
		{
			ImGuiCustom.Property("Progress", render.Progress.ToInvariantPercent());
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

		if (ImGuiCustom.OpenHeader("Destination"))
		{
			RenderTexture texture = render.texture;

			if (ImGuiCustom.BeginProperties())
			{
				ImGuiCustom.Property("Resolution", texture.size.ToInvariant());
				ImGuiCustom.Property("Tile Size", texture.tileSize.ToInvariant());

				ImGuiCustom.EndProperties();
			}

			if (ImGui.BeginTable("Layers", 4, ImGuiCustom.DefaultTableFlags))
			{
				ImGui.TableSetupColumn("Label");
				ImGui.TableSetupColumn("Type");
				ImGui.TableSetupColumn("Color");
				ImGui.TableSetupColumn("Actions");
				ImGui.TableHeadersRow();

				foreach ((string label, TextureGrid layer) in texture.Layers)
				{
					ImGuiCustom.TableItem(label);
					Type type = layer.GetType();

					ImGuiCustom.TableItem(type.Name);
					ImGuiCustom.TableItem(type.IsGenericType ? type.GetGenericArguments()[0].Name : "");

					ImGui.TableNextColumn();
					ImGui.SmallButton("View"); //TODO
					ImGui.SameLine();
					ImGui.SmallButton("Save"); //TODO
				}

				ImGui.EndTable();
			}
		}
	}

	bool DrawOperation(Operation operation, bool select)
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
			if (select) flags |= ImGuiTabItemFlags.SetSelected;

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

		if (ImGuiCustom.OpenHeader("Overview") && ImGuiCustom.BeginProperties())
		{
			ImGuiCustom.Property("Total Infinite Light", scene.infiniteLights.Length.ToInvariant());
			ImGuiCustom.Property("Infinite Lights Power", scene.infiniteLightsPower.ToInvariant());
			ImGuiCustom.Property("All Lights Power", (scene.infiniteLightsPower + scene.lightPicker.Power).ToInvariant());

			ImGuiCustom.PropertySeparator();

			ImGuiCustom.Property("Camera Position", scene.camera.ContainedPosition.ToInvariant());
			ImGuiCustom.Property("Camera Forward", (scene.camera.ContainedRotation * Float3.Forward).ToInvariant());
			ImGuiCustom.Property("Camera Up", (scene.camera.ContainedRotation * Float3.Up).ToInvariant());
			ImGuiCustom.Property("Camera Field of View", scene.camera.FieldOfView.ToInvariant());

			ImGuiCustom.EndProperties();
		}

		PreparedPack pack = scene;

		if (ImGuiCustom.OpenHeader("Geometries"))
		{
			if (ImGuiCustom.BeginProperties())
			{
				ImGuiCustom.Property("Enclosing Box", pack.accelerator.BoxBound.ToInvariant());
				ImGuiCustom.Property("Enclosing Sphere", pack.accelerator.SphereBound.ToInvariant());
				ImGuiCustom.Property("Accelerator", pack.accelerator.GetType().Name);

				ImGuiCustom.EndProperties();
			}

			if (ImGui.BeginTable("Geometries Table", 4, ImGuiCustom.DefaultTableFlags))
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
		}

		if (ImGuiCustom.OpenHeader("Lights") && ImGuiCustom.BeginProperties())
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
	}

	void DrawOperation(EvaluationOperation operation)
	{
		double progress = operation.Progress;
		TimeSpan time = operation.Time;

		if (ImGuiCustom.OpenHeader("Overview") && ImGuiCustom.BeginProperties())
		{
			ImGuiCustom.Property("Progress", progress.ToInvariantPercent());
			ImGuiCustom.Property("Completed", operation.IsCompleted.ToString());
			ImGuiCustom.Property("Creation Time", operation.creationTime.ToInvariant());
			ImGuiCustom.Property("Total Workload", operation.TotalProcedureCount.ToInvariant());

			ImGuiCustom.PropertySeparator();

			ImGuiCustom.Property("Time Spent", time.ToInvariant());
			ImGuiCustom.Property("Time Spent (All Worker)", operation.TotalTime.ToInvariant());

			if (progress is > 0f and < 1f)
			{
				TimeSpan timeRemain = time / progress - time;
				DateTime timeFinish = DateTime.Now + timeRemain;

				ImGuiCustom.Property("Estimated Time Remain", timeRemain.ToInvariant());
				ImGuiCustom.Property("Estimated Completion Time", timeFinish.ToInvariant());
			}

			ImGuiCustom.EndProperties();
		}

		if (ImGuiCustom.OpenHeader("Profile") && ImGuiCustom.BeginProperties())
		{
			EvaluationProfile profile = operation.profile;

			ImGuiCustom.Property("Evaluator", profile.Evaluator.GetType().Name);
			ImGuiCustom.Property("Distribution", profile.Distribution.GetType().Name);
			ImGuiCustom.Property("Destination", profile.TargetLayer);

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();
			if (ImGui.SmallButton("View Layer")) root.Find<ViewerUI>().Track(operation);

			ImGuiCustom.PropertySeparator();

			ImGuiCustom.Property("Epoch Size", profile.Distribution.Extend.ToInvariant());
			ImGuiCustom.Property("Min Epoch Count", profile.MinEpoch.ToInvariant());
			ImGuiCustom.Property("Max Epoch Count", profile.MaxEpoch.ToInvariant());
			ImGuiCustom.Property("Noise Threshold", profile.NoiseThreshold.ToString("E2", InvariantFormat.Culture));

			ImGuiCustom.EndProperties();
		}

		if (operation.EventRowCount > 0 && ImGuiCustom.OpenHeader("Events") &&
			ImGui.BeginTable("Events Table", 4, ImGuiCustom.DefaultTableFlags))
		{
			double timeR = 1d / time.TotalSeconds;
			double progressR = 1d / progress;
			bool divideByZero = time == TimeSpan.Zero || progress.AlmostEquals();

			Utility.EnsureCapacity(ref eventRows, operation.EventRowCount);

			SpanFill<EventRow> fill = eventRows;
			operation.FillEventRows(ref fill);

			ImGui.TableSetupColumn("Label");
			ImGui.TableSetupColumn("Total Done");
			ImGui.TableSetupColumn("Per Second");
			ImGui.TableSetupColumn("Estimate");
			ImGui.TableHeadersRow();

			foreach ((string label, ulong count) in fill.Filled)
			{
				ImGuiCustom.TableItem(label);
				ImGuiCustom.TableItem(count.ToInvariant());

				if (!divideByZero)
				{
					ImGuiCustom.TableItem(((float)(count * timeR)).ToInvariant());
					ImGuiCustom.TableItem(((ulong)(count * progressR)).ToInvariant());
				}
			}

			ImGui.EndTable();
		}
	}

	void DrawOperation(CompositionOperation operation) { }
}