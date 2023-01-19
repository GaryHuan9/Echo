using System;
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
	string currentFile;
	bool saveMode;
	Action<string> openAction;

	protected override string Name => throw new NotSupportedException();

	bool IsOpen => openAction != null;

	public void Open(string path, bool load, Action<string> action)
	{
		Ensure.IsFalse(IsOpen);
		Ensure.IsNotNull(action);

		currentPath = Path.GetFullPath(path);
		currentFile = Path.GetFileName(path);
		saveMode = !load;
		openAction = action;
	}

	public override void NewFrame(in Moment moment)
	{
		if (!IsOpen) return;

		string name = saveMode ? "Save File" : "Load File";
		ImGui.OpenPopup(name);
		bool open = true;

		var size = ImGui.GetMainViewport().Size;
		ImGui.SetNextWindowSize(size * 0.3f, ImGuiCond.Appearing);
		ImGui.SetNextWindowPos(size * 0.35f, ImGuiCond.Appearing);
		ImGui.BeginPopupModal(name, ref open, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

		if (!open) CloseDialogue();
		else NewFrameWindow(moment);

		ImGui.EndPopup();
	}

	protected override void NewFrameWindow(in Moment moment)
	{
		DirectoryInfo currentParent = Directory.GetParent(currentPath);
		ImGui.BeginDisabled(currentParent == null);
		if (ImGui.Button("^")) currentPath = currentParent!.FullName;
		ImGui.EndDisabled();

		ImGui.SameLine();
		ImGui.TextUnformatted(currentPath);

		var tableSize = new Vector2(0f, ImGui.GetWindowSize().Y - ImGui.GetTextLineHeightWithSpacing() * 4f);
		if (ImGui.BeginTable("Entries", 4, ImGuiCustom.DefaultTableFlags | ImGuiTableFlags.ScrollY, tableSize))
		{
			ImGui.TableSetupColumn("Name");
			ImGui.TableSetupColumn("Type");
			ImGui.TableSetupColumn("Size");
			ImGui.TableSetupColumn("Date Modified");
			ImGui.TableHeadersRow();

			foreach (string path in Directory.EnumerateDirectories(currentPath))
			{
				var info = new DirectoryInfo(path);

				ImGui.TableNextColumn();
				ImGui.TextDisabled(info.Name);
				ImGuiCustom.TableItem("Folder");
				ImGuiCustom.TableItem("---");
				ImGuiCustom.TableItem(info.LastWriteTime.ToString("G"));

				DrawSelectable($"##{info.Name}", out bool doubleClicked);

				if (doubleClicked) currentPath = info.FullName;
			}

			foreach (string path in Directory.EnumerateFiles(currentPath))
			{
				var info = new FileInfo(path);

				ImGui.TableNextColumn();
				ImGui.TextUnformatted(info.Name);
				ImGuiCustom.TableItem(info.Extension);
				ImGuiCustom.TableItem(info.Length.ToInvariant());
				ImGuiCustom.TableItem(info.LastWriteTime.ToString("G"));

				if (DrawSelectable($"##{info.Name}", out bool doubleClicked))
				{
					currentFile = info.Name;
					if (doubleClicked) OpenPath();
				}
			}

			ImGui.EndTable();
		}

		bool fileExists = File.Exists(Path.Combine(currentPath, currentFile));

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

		if (ImGui.InputText("##Path", ref currentFile, 256, ImGuiInputTextFlags.EnterReturnsTrue |
															ImGuiInputTextFlags.AutoSelectAll) &&
			File.Exists(Path.Combine(currentPath, currentFile))) OpenPath();

		ImGui.PopItemWidth();

		static bool DrawSelectable(string label, out bool doubleClicked)
		{
			ImGui.SameLine();
			bool clicked = ImGui.Selectable(label, false, ImGuiSelectableFlags.SpanAllColumns |
														  ImGuiSelectableFlags.AllowDoubleClick);
			doubleClicked = clicked && ImGui.IsMouseDoubleClicked(0);
			return clicked;
		}
	}

	void CloseDialogue()
	{
		ImGui.CloseCurrentPopup();
		openAction = null;
	}

	void OpenPath()
	{
		Ensure.IsTrue(IsOpen);
		openAction(Path.GetFullPath(Path.Combine(currentPath, currentFile)));
		CloseDialogue();
	}
}