using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Echo.Core.Common.Compute;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Processes;
using Echo.UserInterface.Core.Common;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public sealed class SchedulerUI : AreaUI
{
	public SchedulerUI(EchoUI root) : base(root) { }

	SystemUI systemUI;
	FileUI dialogueUI;
	RenderUI renderUI;

	Device currentDevice;
	string currentPath;
	FileSystemWatcher fileWatcher;

	bool fileExists;
	bool fileChanged;

	EchoSource loadedFile;
	DateTime loadTime;
	int currentProfile;
	bool autoSchedule = true;
	bool autoAbort = true;

	readonly List<string> profileStrings = new();
	readonly List<int> profileIndices = new();
	readonly List<ScheduledRender> allScheduled = new();

	RenderProfile constructedProfile;

	const string EchoFileExtension = ".echo";

	protected override string Name => "Scheduler";

	public override void Initialize()
	{
		base.Initialize();

		systemUI = root.Find<SystemUI>();
		dialogueUI = root.Find<FileUI>();
		renderUI = root.Find<RenderUI>();

		string[] arguments = Environment.GetCommandLineArgs();
		string path = "ext/Scenes/";

		if (arguments.Length > 1) path = arguments[1];

		ChangeCurrentPath(path);
	}

	protected override void NewFrameWindow()
	{
		Device device = systemUI.Device;
		if (currentDevice != device) renderUI.ClearRenders();
		currentDevice = device;

		if (!fileExists) DrawFileMarker("(!)", $"Not an {EchoFileExtension} file.");
		else if (fileChanged) DrawFileMarker("(*)", "File changed externally.");

		ImGui.TextWrapped(currentPath);

		if (ImGui.Button("Locate")) dialogueUI.Open(currentPath, true, ChangeCurrentPath);

		ImGui.BeginDisabled(!fileExists);
		ImGui.SameLine();
		if (ImGui.Button("Load")) LoadEchoFile();
		ImGui.EndDisabled();

		ImGui.BeginDisabled(loadedFile == null);
		ImGui.SameLine();
		if (ImGui.Button("Schedule")) ConstructRenderProfile();
		ImGui.EndDisabled();

		if (loadedFile != null)
		{
			ImGui.TextUnformatted($"File loaded at {loadTime:T}.");
			ImGuiCustom.Selector("Profile", CollectionsMarshal.AsSpan(profileStrings), ref currentProfile);
		}

		ImGui.Checkbox("Auto Schedule on File Change", ref autoSchedule);
		ImGuiCustom.HelpTip("Whether to automatically schedule with current settings when the file changes externally.");

		ImGui.Checkbox("Auto Abort on New Schedule", ref autoAbort);
		ImGuiCustom.HelpTip("Whether to automatically abort all previous unfinished renders when a new render is scheduled.");

		if (autoSchedule && fileExists && fileChanged && LoadEchoFile()) ConstructRenderProfile();

		var profile = Interlocked.Exchange(ref constructedProfile, null);
		if (profile != null) ScheduleRenderProfile(profile);
	}

	void ChangeCurrentPath(string path)
	{
		if (fileWatcher != null)
		{
			fileWatcher.EnableRaisingEvents = false;
			fileWatcher.Dispose();
			fileWatcher = null;
		}

		currentPath = Path.GetRelativePath(Environment.CurrentDirectory, path);
		fileExists = Path.GetExtension(path) == EchoFileExtension && File.Exists(path);

		loadedFile = null;
		if (!fileExists) return;

		fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(path)!, Path.GetFileName(path)!);

		fileWatcher.Changed += (_, _) => fileChanged = true;
		fileWatcher.EnableRaisingEvents = true;
		fileChanged = true;
	}

	bool LoadEchoFile()
	{
		string path = Path.GetFullPath(currentPath);
		string error;

		try
		{
			fileChanged = false;
			loadedFile = new EchoSource(path);
			loadTime = DateTime.Now;

			profileStrings.Clear();
			profileIndices.Clear();

			for (int i = 0; i < loadedFile.Length; i++)
			{
				EchoSource.Entry entry = loadedFile[i];
				if (!entry.CanConstructAs<RenderProfile>()) continue;

				profileStrings.Add($"{entry.Identifier} [{entry.Type.Name}]");
				profileIndices.Add(i);
			}

			if (profileStrings.Count > 0) return true;

			error = $"No {nameof(RenderProfile)} in {nameof(EchoSource)} at '{path}'.";
			loadedFile = null;
		}
		catch (FileNotFoundException) { error = $"No appropriate {nameof(EchoSource)} exists at path '{path}'."; }
		catch (FormatException exception) { error = $"Encountered syntax error when parsing '{path}': {exception}"; }

		LogList.AddError(error);
		return false;
	}

	void ConstructRenderProfile()
	{
		if (currentProfile >= profileIndices.Count) return;
		var entry = loadedFile[profileIndices[currentProfile]];

		ActionQueue.Enqueue("Construct Profile", () =>
		{
			try
			{
				var profile = entry.Construct<RenderProfile>();
				Volatile.Write(ref constructedProfile, profile);
			}
			catch (FormatException exception) { LogList.AddError($"Encountered semantic error: {exception}"); }
		});
	}

	void ScheduleRenderProfile(RenderProfile profile)
	{
		if (autoAbort)
		{
			foreach (ScheduledRender previous in allScheduled) previous.Abort();
			allScheduled.Clear();
		}

		var render = profile.ScheduleTo(currentDevice);

		allScheduled.Add(render);
		renderUI.AddRender(render);
	}

	static void DrawFileMarker(string symbol, string help)
	{
		ImGui.TextDisabled(symbol);

		if (ImGui.IsItemHovered())
		{
			ImGui.BeginTooltip();
			ImGui.TextUnformatted(help);
			ImGui.EndTooltip();
		}

		ImGui.SameLine();
	}
}