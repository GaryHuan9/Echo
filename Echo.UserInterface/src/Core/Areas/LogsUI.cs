using System.IO;
using Echo.Core.Common.Diagnostics;
using Echo.UserInterface.Backend;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public sealed class LogsUI : AreaUI
{
	public LogsUI(EchoUI root) : base(root) { }

	bool autoScroll = true;

	public override string Name => "Logs";

	public override void Initialize()
	{
		base.Initialize();
		DebugHelper.Logger = LogList.Logger;
	}

	public override void NewFrame(in Moment moment)
	{
		if (ImGui.Button("Save to Disk")) SaveToFile();

		ImGui.SameLine();
		if (ImGui.Button("Clear All")) LogList.Clear();

		ImGui.SameLine();
		ImGui.Checkbox("Auto Scroll", ref autoScroll);

		ImGui.Separator();
		ImGui.BeginChild("History");

		foreach (string log in LogList.Logs) ImGui.TextWrapped(log);

		if (autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY()) ImGui.SetScrollHereY(1f);

		ImGui.EndChild();
	}

	static void SaveToFile() => ActionQueue.Enqueue("Log List Dump", () =>
	{
		using var writer = new StreamWriter("logs.txt");
		foreach (string log in LogList.Logs) writer.WriteLine(log);
	});
}