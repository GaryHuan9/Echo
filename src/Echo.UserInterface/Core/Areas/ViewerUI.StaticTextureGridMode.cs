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
			uv = texture.ToUV(position);
			RGBA128 color = texture[uv];

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
							Float2 uv = texture.ToUV(new Int2(x, y));
							total += texture[uv] - (Float4)compare[uv];
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
			var filter = texture.Filter;
			texture.Filter = IFilter.point;

			LockDisplay(out uint* pixels, out nint stride);

			for (int y = 0; y < texture.size.Y; y++, pixels += stride)
			for (int x = 0; x < texture.size.X; x++)
			{
				Float2 uv = texture.ToUV(new Int2(x, y));
				pixels[x] = ColorToUInt32(texture[uv]);
			}

			UnlockDisplay();

			texture.Filter = filter;
		}

		unsafe void FillDisplayCompare()
		{
			LockDisplay(out uint* pixels, out nint stride);

			for (int y = 0; y < texture.size.Y; y++, pixels += stride)
			for (int x = 0; x < texture.size.X; x++)
			{
				Float2 uv = texture.ToUV(new Int2(x, y));
				Float4 color = texture[uv];

				color = Float4.Half + color - compare[uv];
				color += Float4.Ana; //Force alpha to be 1
				pixels[x] = ColorToUInt32(color);
			}

			UnlockDisplay();
		}
	}
}