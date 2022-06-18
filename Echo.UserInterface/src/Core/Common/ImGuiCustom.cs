using System.Numerics;
using System.Runtime.CompilerServices;
using CodeHelpers.Mathematics;
using ImGuiNET;

namespace Echo.UserInterface.Core.Common;

public static class ImGuiCustom
{
	public const ImGuiTableFlags DefaultTableFlags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg |
													 ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.Reorderable |
													 ImGuiTableFlags.BordersOuter;

	const ImGuiTableFlags PropertiesTableFlags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoSavedSettings |
												 ImGuiTableFlags.Resizable | ImGuiTableFlags.NoBordersInBodyUntilResize;

	public static void TableItem(string value, bool wrap = false)
	{
		ImGui.TableNextColumn();
		if (wrap) ImGui.TextWrapped(value);
		else ImGui.TextUnformatted(value);
	}

	public static bool BeginProperties(string name)
	{
		if (!ImGui.BeginTable(name, 2, PropertiesTableFlags)) return false;

		ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.None, 2f);
		ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.None, 3f);

		return true;
	}

	public static void Property(string label, string value)
	{
		TableItem(label);
		TableItem(value);
	}

	public static void EndProperties() => ImGui.EndTable();

	public static Vector4 GetColor(ImGuiCol color = ImGuiCol.CheckMark) => ImGui.GetStyle().Colors[(int)color];

	public static uint GetColorInteger(ImGuiCol color = ImGuiCol.CheckMark)
	{
		Vector4 c = GetColor(color);
		var converted = new Color32(c.X, c.Y, c.Z, c.W);
		return Unsafe.As<Color32, uint>(ref converted);
	}
}