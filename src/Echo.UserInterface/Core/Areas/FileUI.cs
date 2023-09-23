using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Echo.Core.Common.Diagnostics;
using Echo.Core.InOut;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public sealed class FileUI : AreaUI
{
	public FileUI(EchoUI root) : base(root) { }

	string currentPath;
	string currentName;
	bool saveMode;
	Action<string> openAction;

	protected override string Name => throw new NotSupportedException();

	bool IsOpen => openAction != null;

	string CurrentFile => Path.Combine(currentPath, currentName);

	public void Open(string path, bool load, Action<string> action)
	{
		Ensure.IsFalse(IsOpen);
		Ensure.IsNotNull(action);
		Ensure.IsFalse(string.IsNullOrEmpty(path));

		currentPath = Path.GetFullPath(path);

		if (!Directory.Exists(currentPath))
		{
			currentName = Path.GetFileName(currentPath);
			currentPath = Path.GetDirectoryName(currentPath);
		}
		else currentName = "";

		saveMode = !load;
		openAction = action;
	}

	public override void NewFrame()
	{
		if (!IsOpen) return;

		string name = saveMode ? "Save File" : "Load File";
		ImGui.OpenPopup(name);
		bool open = true;

		var size = ImGui.GetMainViewport().Size;
		ImGui.SetNextWindowSize(size * 0.4f, ImGuiCond.Appearing);
		ImGui.SetNextWindowPos(size * 0.3f, ImGuiCond.Appearing);
		ImGui.BeginPopupModal(name, ref open, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

		if (!open) CloseDialogue();
		else NewFrameWindow();

		ImGui.EndPopup();
	}

	protected override void NewFrameWindow()
	{
		DirectoryInfo current = new DirectoryInfo(currentPath);

		DirectoryInfo parent = current.Parent;
		ImGui.BeginDisabled(parent == null);
		if (ImGui.Button("^")) currentPath = parent!.FullName;
		ImGui.EndDisabled();

		bool exists = current.Exists;
		ImGui.BeginDisabled(!exists);
		ImGui.SameLine();
		if (ImGui.Button("+")) OpenFileExplorer(current.FullName);
		ImGui.EndDisabled();

		ImGui.SameLine();
		ImGui.PushItemWidth(-1f);
		string path = current.FullName;
		uint length = Math.Max(256, (uint)path.Length * 2);
		if (ImGui.InputText("##path", ref path, length)) currentPath = path;
		ImGui.PopItemWidth();

		float tableHeight = ImGui.GetContentRegionAvail().Y - ImGui.GetTextLineHeightWithSpacing();

		if (exists) DrawTable(tableHeight, current);
		else DrawNoDirectoryTable(tableHeight);

		bool fileExists = File.Exists(CurrentFile);

		if (!saveMode)
		{
			ImGui.BeginDisabled(!fileExists);
			if (ImGui.Button("Load")) OpenPath();
			ImGui.EndDisabled();
		}
		else if (ImGui.Button(fileExists ? "Overwrite" : "Save")) OpenPath();

		ImGui.SameLine();
		if (ImGui.Button("Cancel")) CloseDialogue();

		ImGui.SameLine();
		ImGui.PushItemWidth(ImGuiCustom.UseAvailable);

		if (ImGui.InputText("##Path", ref currentName, 256, ImGuiInputTextFlags.EnterReturnsTrue |
															ImGuiInputTextFlags.AutoSelectAll) &&
			File.Exists(CurrentFile)) OpenPath();

		ImGui.PopItemWidth();
	}

	void CloseDialogue()
	{
		ImGui.CloseCurrentPopup();
		openAction = null;
	}

	void OpenPath()
	{
		Ensure.IsTrue(IsOpen);
		openAction(Path.GetFullPath(CurrentFile));
		CloseDialogue();
	}

	void DrawTable(float tableHeight, DirectoryInfo current)
	{
		if (!ImGui.BeginTable("Entries", 4, ImGuiCustom.DefaultTableFlags | ImGuiTableFlags.ScrollY, new Vector2(0f, tableHeight))) return;

		ImGui.TableSetupScrollFreeze(0, 1);
		ImGui.TableSetupColumn("Name");
		ImGui.TableSetupColumn("Type");
		ImGui.TableSetupColumn("Size");
		ImGui.TableSetupColumn("Date Modified");
		ImGui.TableHeadersRow();

		foreach (DirectoryInfo info in current.EnumerateDirectories())
		{
			ImGui.TableNextColumn();
			ImGui.TextDisabled(info.Name);
			ImGuiCustom.TableItem("Folder");
			ImGuiCustom.TableItem("---");
			ImGuiCustom.TableItem(info.LastWriteTime.ToString("G"));

			DrawSelectable($"##{info.Name}", out bool doubleClicked);
			if (doubleClicked) currentPath = info.FullName;
		}

		foreach (FileInfo info in current.EnumerateFiles())
		{
			ImGui.TableNextColumn();
			ImGui.TextUnformatted(info.Name);
			ImGuiCustom.TableItem(info.Extension);
			ImGuiCustom.TableItem(info.Length.ToInvariant());
			ImGuiCustom.TableItem(info.LastWriteTime.ToString("G"));

			if (DrawSelectable($"##{info.Name}", out bool doubleClicked))
			{
				currentName = info.Name;
				if (doubleClicked) OpenPath();
			}
		}

		ImGui.EndTable();

		static bool DrawSelectable(string label, out bool doubleClicked)
		{
			ImGui.SameLine();
			bool clicked = ImGui.Selectable(label, false, ImGuiSelectableFlags.SpanAllColumns |
														  ImGuiSelectableFlags.AllowDoubleClick);
			doubleClicked = clicked && ImGui.IsMouseDoubleClicked(0);
			return clicked;
		}
	}

	static void OpenFileExplorer(string path)
	{
		if (!Path.EndsInDirectorySeparator(path)) path += Path.DirectorySeparatorChar;
		Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
	}

	static void DrawNoDirectoryTable(float tableHeight)
	{
		if (!ImGui.BeginTable("Dummy", 1, ImGuiCustom.DefaultTableFlags, new Vector2(0f, tableHeight))) return;

		ImGui.TableNextColumn();
		ImGui.TextUnformatted("Directory does not exist.");
		ImGui.EndTable();
	}
}