using ImGuiNET;

namespace Echo.UserInterface.Core.Common;

public static class ImGuiCustom
{
	public const ImGuiTableFlags DefaultTableFlags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg |
													 ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.Reorderable |
													 ImGuiTableFlags.Borders;

	public static void TableItem(string value, bool wrap = false)
	{
		ImGui.TableNextColumn();
		if (wrap) ImGui.TextWrapped(value);
		else ImGui.TextUnformatted(value);
	}

	public static bool BeginProperties(string name) => ImGui.BeginTable(name, 2, ImGuiTableFlags.BordersOuter);

	public static void Property(string label, string value)
	{
		TableItem(label);
		TableItem(value);
	}

	public static void EndProperties() => ImGui.EndTable();
}