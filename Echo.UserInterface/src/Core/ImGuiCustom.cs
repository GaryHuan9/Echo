using ImGuiNET;

namespace Echo.UserInterface.Core;

public static class ImGuiCustom
{
	public const ImGuiTableFlags DefaultTableFlags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg |
													 ImGuiTableFlags.BordersOuter | ImGuiTableFlags.Reorderable |
													 ImGuiTableFlags.NoSavedSettings;

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