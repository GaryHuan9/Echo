using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.InOut;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

partial class ViewerUI
{
	sealed class StaticTextureGridMode : Mode
	{
		public StaticTextureGridMode(EchoUI root) : base(root) { }

		FileUI dialogue;

		TextureGrid texture;
		TextureGrid compare;

		public override float AspectRatio => texture.aspects.X;

		public void Reset(TextureGrid newTexture)
		{
			dialogue ??= root.Find<FileUI>();
			texture = newTexture;
			compare = null;

			Int2 size = texture.size;
			RecreateDisplay(size);
			UpdateDisplayContent();
		}

		public override bool Draw(ImDrawListPtr drawList, in Bounds plane, Float2? cursorUV)
		{
			DrawDisplay(drawList, plane);
			if (cursorUV == null) return true;

			Float2 uv = cursorUV.Value;
			Int2 position = texture.ToPosition(uv);
			RGBA128 color = texture[position];

			ImGui.BeginTooltip();

			ImGui.TextUnformatted($"Location: {position.ToInvariant()}");
			ImGui.TextUnformatted(compare == null ?
				$"Color: {color.ToInvariant()}" :
				$"Delta: {(color - (Float4)compare[uv]).ToInvariant()}");

			ImGui.EndTooltip();
			return true;
		}

		public override void DrawMenuBar()
		{
			if (ImGui.BeginMenu("View"))
			{
				if (ImGui.MenuItem("Update")) UpdateDisplayContent();
				ImGui.EndMenu();
			}

			if (ImGui.BeginMenu("Comparison"))
			{
				if (compare == null)
				{
					if (ImGui.MenuItem("Begin"))
					{
						dialogue.Open("main.png", true, path =>
						{
							compare = texture.Load(path);
							UpdateDisplayContent();
						});
					}
				}
				else
				{
					if (ImGui.MenuItem("Print Total Delta"))
					{
						Summation total = Summation.Zero;

						for (int y = 0; y < texture.size.Y; y++)
						for (int x = 0; x < texture.size.X; x++)
						{
							Int2 position = new Int2(x, y);
							Float2 uv = texture.ToUV(position);
							total += texture[position] - (Float4)compare[uv];
						}

						LogList.Add($"Color delta versus reference: {total.Result:N4}.");
					}

					if (ImGui.MenuItem("End"))
					{
						compare = null;
						UpdateDisplayContent();
					}
				}

				ImGui.EndMenu();
			}
		}

		void UpdateDisplayContent() => ActionQueue.Enqueue("Fill Viewer Display", compare == null ? FillDisplay : FillDisplayCompare);

		unsafe void FillDisplay()
		{
			LockDisplay(out uint* pixels);
			int height = texture.oneLess.Y;

			for (int y = 0; y < texture.size.Y; y++)
			for (int x = 0; x < texture.size.X; x++)
			{
				Int2 position = new Int2(x, height - y);
				*pixels++ = ColorToUInt32(texture[position]);
			}

			UnlockDisplay();
		}

		unsafe void FillDisplayCompare()
		{
			LockDisplay(out uint* pixels);
			int height = texture.oneLess.Y;

			for (int y = 0; y < texture.size.Y; y++)
			for (int x = 0; x < texture.size.X; x++)
			{
				Int2 position = new Int2(x, height - y);
				Float2 uv = texture.ToUV(position);
				Float4 color = texture[position];

				color = Float4.Half + color - compare[uv];
				color += Float4.Ana; //Force alpha to be 1
				*pixels++ = ColorToUInt32(color);
			}

			UnlockDisplay();
		}
	}
}