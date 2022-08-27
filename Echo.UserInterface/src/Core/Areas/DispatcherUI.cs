using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Evaluation.Operation;
using Echo.Core.InOut;
using Echo.Core.Scenic.Hierarchies;
using Echo.Core.Scenic.Preparation;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public class DispatcherUI : AreaUI
{
	public DispatcherUI() : base("Dispatcher") { }

	string filePath = "ext/Scenes/";
	readonly List<string> pathCandidates = new();

	EchoChronicleHierarchyObjects objects;
	readonly List<string> sceneLabels = new();
	readonly List<string> profileLabels = new();

	int sceneIndex;
	int profileIndex;

	static readonly EnumerationOptions enumerationOptions = new()
	{
		MatchCasing = MatchCasing.CaseInsensitive,
		RecurseSubdirectories = false
	};

	public override void Initialize()
	{
		base.Initialize();

		string[] arguments = Environment.GetCommandLineArgs();

		if (arguments.Length > 1)
		{
			filePath = Path.GetRelativePath(Environment.CurrentDirectory, arguments[1]);

			ReadFile();
			Dispatch(Device.CreateOrGet());
		}
	}

	protected override unsafe void Update(in Moment moment)
	{
		var device = Device.Instance;
		if (device == null) return;

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
		if (ImGui.Button("Load and Dispatch"))
		{
			ReadFile();
			Dispatch(device);
		}

		if (objects == null)
		{
			ImGui.TextUnformatted("Load and dispatch a valid .echo file to begin evaluation!");
			return;
		}

		ImGuiCustom.Selector("Scene", CollectionsMarshal.AsSpan(sceneLabels), ref sceneIndex);
		ImGuiCustom.Selector("Profile", CollectionsMarshal.AsSpan(profileLabels), ref profileIndex);

		Vector2 buttonSize = new Vector2(ImGui.CalcItemWidth(), 0f);
		if (ImGui.Button("Dispatch to Active Device", buttonSize)) Dispatch(device);
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

		pathCandidates.AddRange(from path in Directory.EnumerateFiles(parent, $"{current}*.echo", enumerationOptions)
								select Path.GetFileName(path));
		pathCandidates.AddRange(from path in Directory.EnumerateDirectories(parent, $"{current}*", enumerationOptions)
								select $"{Path.GetFileName(path)}/");
	}

	void ReadFile()
	{
		var read = ReadFile(filePath);
		if (read == null) return;

		PopulateLabels<Scene>(read, sceneLabels);
		PopulateLabels<EvaluationProfile>(read, profileLabels);

		objects = read;
	}

	void Dispatch(Device device)
	{
		if (objects == null) return;

		string sceneLabel = sceneLabels.Count == 0 ? null : sceneLabels[sceneIndex];
		string profileLabel = profileLabels.Count == 0 ? null : profileLabels[profileIndex];

		if (sceneLabel == null || profileLabel == null)
		{
			LogList.AddError($"Missing specified {nameof(Scene)} or {nameof(EvaluationProfile)} to dispatch.");
			return;
		}

		var scene = ConstructFirst<Scene>(objects, sceneLabel);
		var profile = ConstructFirst<EvaluationProfile>(objects, profileLabel);

		ActionQueue.Enqueue("Evaluation Operation Dispatch", () =>
		{
			var preparer = new ScenePreparer(scene);
			var operation = new EvaluationOperation.Factory
			{
				NextScene = preparer.Prepare(),
				NextProfile = profile
			};

			device.Dispatch(operation);
		});

		static T ConstructFirst<T>(EchoChronicleHierarchyObjects objects, string label) where T : class
		{
			for (int i = 0; i < objects.Length; i++)
			{
				EchoChronicleHierarchyObjects.Entry entry = objects[i];
				if (!label.AsSpan().StartsWith(entry.Identifier)) continue;

				T constructed = entry.Construct<T>();
				Ensure.IsNotNull(constructed);
				return constructed;
			}

			return null;
		}
	}

	static EchoChronicleHierarchyObjects ReadFile(string path)
	{
		if (!File.Exists(path))
		{
			LogList.AddError($"No appropriate scene file exists at path '{path}'.");
			return null;
		}

		path = Path.GetFullPath(path);

		try
		{
			return new EchoChronicleHierarchyObjects(path);
		}
		catch (FormatException exception)
		{
			LogList.AddError($"Encountered syntax error when parsing '{path}': {exception}");
			return null;
		}
	}

	static void PopulateLabels<T>(EchoChronicleHierarchyObjects objects, List<string> labels)
	{
		labels.Clear();

		for (int i = 0; i < objects.Length; i++)
		{
			EchoChronicleHierarchyObjects.Entry entry = objects[i];
			if (!entry.Type.IsAssignableTo(typeof(T))) continue;
			labels.Add($"{entry.Identifier} [{entry.Type.Name}]");
		}
	}
}