using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Diagnostics;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Processes;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public sealed class SchedulerUI : AreaUI
{
	public SchedulerUI(EchoUI root) : base(root) { }

	SystemUI system;

	string filePath = "ext/Scenes/";
	readonly List<string> pathCandidates = new();

	EchoSource objects;
	readonly List<string> profileLabels = new();
	int profileIndex;

	ScheduledRender scheduledRender;

	static readonly EnumerationOptions enumerationOptions = new()
	{
		MatchCasing = MatchCasing.CaseInsensitive,
		RecurseSubdirectories = false
	};

	public override string Name => "Scheduler";

	public override void Initialize()
	{
		base.Initialize();

		system = root.Find<SystemUI>();

		string[] arguments = Environment.GetCommandLineArgs();

		if (arguments.Length > 1)
		{
			filePath = arguments[1];

			if (arguments.Length > 2 && arguments[2] == "build-path")
			{
				const string ProjectPath = "../../../../Echo.Core/";
				filePath = Path.Combine(ProjectPath, filePath);
			}

			filePath = Path.GetFullPath(filePath, Environment.CurrentDirectory);
			filePath = Path.GetRelativePath(Environment.CurrentDirectory, filePath);

			ReadFile();
			Dispatch(Device.CreateOrGet());
		}
	}

	public override unsafe void NewFrame(in Moment moment)
	{
		//Show path input box
		if (ImGui.InputText("##Path", ref filePath, 128, ImGuiInputTextFlags.CallbackCompletion, CompletionCallback)) PopulatePathCandidates();
		if (ImGui.IsItemClicked()) PopulatePathCandidates();

		//Manage path auto complete
		if (pathCandidates.Count > 0 && ImGui.IsItemActive())
		{
			ImGui.BeginTooltip();
			for (int i = 0; i < pathCandidates.Count; i++) ImGui.TextUnformatted(pathCandidates[i]);
			ImGui.EndTooltip();
		}

		//Display buttons and selector
		ImGui.SameLine();
		if (ImGui.Button("Load")) ReadFile();

		ImGui.SameLine();
		if (ImGui.Button("Load and Schedule"))
		{
			ReadFile();
			Dispatch(system.Device);
		}

		if (objects == null)
		{
			ImGui.TextUnformatted("Load and dispatch a valid .echo file to begin evaluation!");
			return;
		}

		ImGuiCustom.Selector("Profile", CollectionsMarshal.AsSpan(profileLabels), ref profileIndex);

		Vector2 buttonSize = new Vector2(ImGui.CalcItemWidth(), 0f);
		if (ImGui.Button("Schedule to Active Device", buttonSize)) Dispatch(system.Device);
	}

	unsafe int CompletionCallback(ImGuiInputTextCallbackData* pointer)
	{
		ImGuiInputTextCallbackDataPtr data = pointer;
		if (pathCandidates.Count != 1) return 0;

		int length = Path.GetDirectoryName(filePath.AsSpan()).Length + 1;
		data.DeleteChars(length, data.BufTextLen - length);
		data.InsertChars(length, pathCandidates[0]);

		return 0;
	}

	void PopulatePathCandidates()
	{
		pathCandidates.Clear();

		string parent = Path.GetDirectoryName(filePath);
		if (!Directory.Exists(parent)) return;
		string current = Path.GetFileName(filePath);

		pathCandidates.AddRange(
			from path in Directory.EnumerateFiles(parent, $"{current}*.echo", enumerationOptions)
			select Path.GetFileName(path)
		);
		pathCandidates.AddRange(
			from path in Directory.EnumerateDirectories(parent, $"{current}*", enumerationOptions)
			select $"{Path.GetFileName(path)}/"
		);
	}

	void ReadFile()
	{
		var read = ReadFile(filePath);
		if (read == null) return;

		PopulateLabels<RenderProfile>(read, profileLabels);

		objects = read;
		profileIndex = 0;
	}

	void Dispatch(Device device)
	{
		if (objects == null) return;

		if (profileIndex >= profileLabels.Count)
		{
			LogList.AddError($"Missing specified {nameof(RenderProfile)} to dispatch.");
			return;
		}

		var profile = ConstructFirst<RenderProfile>(objects, profileLabels[profileIndex]);

		scheduledRender?.Abort();
		scheduledRender = profile.ScheduleTo(device);

		root.Find<ViewerUI>().Track(scheduledRender.evaluationOperations[0]);
		root.Find<RenderUI>().AddRender(scheduledRender);

		static T ConstructFirst<T>(EchoSource objects, string label) where T : class
		{
			ReadOnlySpan<char> match = label.AsSpan(0, label.LastIndexOf('[')).Trim();
			Ensure.IsFalse(match.IsEmpty);

			for (int i = 0; i < objects.Length; i++)
			{
				EchoSource.Entry entry = objects[i];
				if (!match.SequenceEqual(entry.Identifier)) continue;

				T constructed = entry.Construct<T>();
				Ensure.IsNotNull(constructed);
				return constructed;
			}

			return null;
		}
	}

	static EchoSource ReadFile(string path)
	{
		if (!File.Exists(path))
		{
			LogList.AddError($"No appropriate scene file exists at path '{path}'.");
			return null;
		}

		path = Path.GetFullPath(path);

		try
		{
			return new EchoSource(path);
		}
		catch (FormatException exception)
		{
			LogList.AddError($"Encountered syntax error when parsing '{path}': {exception}");
			return null;
		}
	}

	static void PopulateLabels<T>(EchoSource objects, List<string> labels)
	{
		labels.Clear();

		for (int i = 0; i < objects.Length; i++)
		{
			EchoSource.Entry entry = objects[i];
			if (!entry.Type.IsAssignableTo(typeof(T))) continue;
			labels.Add($"{entry.Identifier} [{entry.Type.Name}]");
		}
	}
}