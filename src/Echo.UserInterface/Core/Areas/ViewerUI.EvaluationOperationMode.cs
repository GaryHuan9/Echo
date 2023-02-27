using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.InOut;
using Echo.Core.Processes.Evaluation;
using Echo.Core.Textures.Evaluation;
using Echo.Core.Textures.Grids;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;
using SDL2;

namespace Echo.UserInterface.Core.Areas;

partial class ViewerUI
{
	sealed class EvaluationOperationMode : Mode
	{
		public EvaluationOperationMode(EchoUI root) : base(root) { }

		EvaluationOperation operation;

		readonly Queue<uint> indexQueue = new();
		uint nextExploreIndex;

		public override float AspectRatio => ((TextureGrid)operation.destination).aspects.X;
		Int2 LayerSize => ((TextureGrid)operation.destination).size;

		public void Reset(EvaluationOperation newOperation)
		{
			if (newOperation == operation) return;

			operation = newOperation;
			RecreateDisplay(LayerSize);
			RestartDisplayUpdate();
		}

		public override bool Draw(ImDrawListPtr drawList, in Bounds plane, Float2? cursorUV)
		{
			if (operation.Disposed) return false;
			
			//Find and update tile from completed procedures
			while (true)
			{
				IEvaluationReadTile tile = RequestUpdateTile();
				if (tile == null) break;
				UpdateDisplay(tile);
			}

			//Actually draw the display
			uint borderColor = ImGuiCustom.GetColorInteger(ImGuiCol.Border);
			drawList.AddRectFilled(plane.MinVector2, plane.MaxVector2, 0xFF000000u);
			drawList.AddImage(Display, plane.MinVector2, plane.MaxVector2);
			drawList.AddRect(plane.MinVector2, plane.MaxVector2, borderColor);

			DrawCurrentTiles(drawList, plane);

			//Display pixel information
			if (cursorUV != null)
			{
				var layer = operation.destination;

				Int2 position = ((TextureGrid)layer).ToPosition(cursorUV.Value);
				Int2 tilePosition = layer.GetTilePosition(position);
				IEvaluationReadTile tile = layer.RequestTile(tilePosition);

				ImGui.BeginTooltip();

				ImGui.TextUnformatted($"Location: {position.ToInvariant()}");
				ImGui.TextUnformatted($"Tile: {tilePosition.ToInvariant()}");
				if (tile != null) ImGui.TextUnformatted($"Color: {tile[position].ToInvariant()}");

				ImGui.EndTooltip();
			}

			return true;
		}

		public override void DrawMenuBar()
		{
			if (ImGui.BeginMenu("View"))
			{
				if (ImGui.MenuItem("Restart")) RestartDisplayUpdate();
				ImGui.EndMenu();
			}
		}

		void RestartDisplayUpdate()
		{
			//Clear queue
			indexQueue.Clear();
			nextExploreIndex = 0;

			//Clear display
			SDL.SDL_LockTexture(Display, IntPtr.Zero, out IntPtr pointer, out int pitch).ThrowOnError();
			SDL.SDL_SetTextureBlendMode(Display, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND).ThrowOnError();

			unsafe
			{
				if (pitch == sizeof(uint) * LayerSize.X) new Span<uint>((uint*)pointer, LayerSize.Product).Fill(0);
				else throw new InvalidOperationException("Fill assumption of contiguous memory from SDL2 is violated!");
			}

			SDL.SDL_UnlockTexture(Display);
		}

		IEvaluationReadTile RequestUpdateTile()
		{
			//Enqueue indices to explore
			while (indexQueue.Count < operation.WorkerCount && nextExploreIndex < operation.TotalProcedureCount)
			{
				indexQueue.Enqueue(nextExploreIndex++);
			}

			//Find the first available tile
			int count = indexQueue.Count;

			for (int i = 0; i < count; i++)
			{
				uint index = indexQueue.Dequeue();
				Int2 tilePosition = operation.tilePositions[(int)index];
				var tile = operation.destination.RequestTile(tilePosition);

				if (tile != null) return tile;
				indexQueue.Enqueue(index);
			}

			return null;
		}

		unsafe void UpdateDisplay(IEvaluationReadTile tile)
		{
			Int2 tileSize = tile.Size;
			if (tileSize == Int2.Zero) return;

			//Grab tile bounds to pixel destination
			Int2 min = tile.Min;
			Int2 max = tile.Max;

			int invertY = LayerSize.Y - min.Y - tileSize.Y;
			SDL.SDL_Rect rect = new SDL.SDL_Rect { x = min.X, y = invertY, w = tileSize.X, h = tileSize.Y };
			SDL.SDL_LockTexture(Display, ref rect, out IntPtr pointer, out int pitch).ThrowOnError();

			int stride = pitch / sizeof(uint);
			uint* pixels = (uint*)pointer - min.X + stride * (tileSize.Y - 1);

			//Convert and assign each pixel
			for (int y = min.Y; y < max.Y; y++, pixels -= stride)
			for (int x = min.X; x < max.X; x++)
			{
				Int2 position = new Int2(x, y);
				Float4 color = tile[position];
				pixels[x] = ColorToUInt32(color);
			}

			SDL.SDL_UnlockTexture(Display);
		}

		[SkipLocalsInit]
		void DrawCurrentTiles(ImDrawListPtr drawList, in Bounds content)
		{
			IEvaluationLayer layer = operation.destination;
			Float2 sizeR = ((TextureGrid)layer).sizeR;

			Float2 invertMin = new Float2(content.min.X, content.max.Y);
			Float2 invertMax = new Float2(content.max.X, content.min.Y);

			uint fillColor = ImGuiCustom.GetColorInteger(ImGuiCol.MenuBarBg);
			uint borderColor = ImGuiCustom.GetColorInteger();

			Span<Procedure> procedures = stackalloc Procedure[operation.WorkerCount];

			SpanFill<Procedure> fill = procedures;
			operation.FillWorkerProcedures(ref fill);

			foreach (ref readonly Procedure procedure in procedures)
			{
				if (procedure.Progress.Equals(0d)) continue;

				Int2 tilePosition = operation.tilePositions[(int)procedure.index];
				layer.GetTileBounds(tilePosition, out Int2 tileMin, out Int2 tileMax);

				Float2 min = Float2.Lerp(invertMin, invertMax, tileMin * sizeR);
				Float2 max = Float2.Lerp(invertMin, invertMax, tileMax * sizeR);

				float progress = (float)procedure.Progress;
				Float2 height = new(max.X, Scalars.Lerp(min.Y, max.Y, progress));
				drawList.AddRectFilled(min.AsVector2(), height.AsVector2(), fillColor);
				drawList.AddRect(min.AsVector2(), max.AsVector2(), borderColor);
			}
		}
	}
}