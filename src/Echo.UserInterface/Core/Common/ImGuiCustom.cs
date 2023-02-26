using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Echo.Core.Common;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using ImGuiNET;

namespace Echo.UserInterface.Core.Common;

public static class ImGuiCustom
{
	public const ImGuiTableFlags DefaultTableFlags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg |
													 ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.Reorderable |
													 ImGuiTableFlags.BordersOuter;

	public const float UseAvailable = -float.Epsilon;
	
	const ImGuiTableFlags PropertiesTableFlags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoSavedSettings |
												 ImGuiTableFlags.Resizable | ImGuiTableFlags.NoBordersInBodyUntilResize;

	const float SectionPadding = 8f;

	public static void TableItem(string value, bool wrap = false)
	{
		ImGui.TableNextColumn();
		if (wrap) ImGui.TextWrapped(value);
		else ImGui.TextUnformatted(value);
	}

	public static bool BeginProperties(string label = "Properties")
	{
		if (!ImGui.BeginTable(label, 2, PropertiesTableFlags)) return false;

		ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.None, 2f);
		ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.None, 3f);

		return true;
	}

	public static void Property(string label, string value)
	{
		TableItem(label);
		TableItem(value);
	}

	public static void PropertySeparator()
	{
		ImGui.TableNextColumn();
		ImGui.Separator();
		ImGui.TableNextColumn();
		ImGui.Separator();
	}

	public static void EndProperties() => ImGui.EndTable();

	public static bool BeginSection(string label)
	{
		ImGui.SetNextItemOpen(true, ImGuiCond.Once);
		if (!ImGui.CollapsingHeader(label)) return false;

		ImGui.PushID(label);
		ImGui.Indent(SectionPadding);

		return true;
	}

	public static void EndSection()
	{
		ImGui.Unindent(SectionPadding);
		ImGui.Dummy(new Vector2(0f, SectionPadding));
		ImGui.PopID();
	}

	public static bool Selector(string label, ReadOnlySpan<string> items, ref int currentIndex)
	{
		int oldIndex = currentIndex;
		currentIndex = currentIndex.Clamp(0, items.Length - 1);
		string preview = items.TryGetValue(currentIndex) ?? "";

		ImGui.PushID(label);

		float width = ImGui.CalcItemWidth();
		float button = ImGui.GetFrameHeight();
		float gap = ImGui.GetStyle().ItemInnerSpacing.X;
		ImGui.SetNextItemWidth(width - gap * 2f - button * 2f);

		if (ImGui.BeginCombo("##Combo", preview, ImGuiComboFlags.NoArrowButton))
		{
			for (int i = 0; i < items.Length; i++)
			{
				bool selected = i == currentIndex;
				if (ImGui.Selectable(items[i], selected)) currentIndex = i;
				if (selected) ImGui.SetItemDefaultFocus();
			}

			ImGui.EndCombo();
		}

		ImGui.SameLine(0f, gap);
		if (ImGui.ArrowButton("##Left", ImGuiDir.Left) && currentIndex > 0) --currentIndex;

		ImGui.SameLine(0f, gap);
		if (ImGui.ArrowButton("##Right", ImGuiDir.Right) && currentIndex < items.Length - 1) ++currentIndex;

		ImGui.PopID();

		ImGui.SameLine(0f, gap);
		ImGui.TextUnformatted(label);

		return currentIndex != oldIndex;
	}

	public static Vector4 GetColor(ImGuiCol color = ImGuiCol.CheckMark) => ImGui.GetStyle().Colors[(int)color];

	public static uint GetColorInteger(ImGuiCol color = ImGuiCol.CheckMark)
	{
		Vector4 c = GetColor(color);
		var converted = new Color32(c.X, c.Y, c.Z, c.W);
		return Unsafe.As<Color32, uint>(ref converted);
	}
}