using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Processes.Evaluation;
using Echo.Core.Textures.Evaluation;
using Echo.Core.Textures.Grids;
using Echo.Core.Textures.Serialization;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;
using SDL2;

namespace Echo.UserInterface.Core.Areas;

public partial class ViewerUI
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

		public override void DrawPlane(ImDrawListPtr drawList, in Bounds plane)
		{
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
			Float2 textureSizeR = 1f / LayerSize;

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

				// if (compareTexture != null)
				// {
				// 	Float2 uv = (position + Float2.Half) * textureSizeR;
				// 	Float4 reference = compareTexture[uv];
				// 	color = Float4.Half + color - reference;
				// 	color += Float4.Ana; //Force alpha to be 1
				// }

				color = ApproximateSqrt(color.Max(Float4.Zero));
				color = color.Min(Float4.One) * byte.MaxValue;
				pixels[x] = SystemSerializer.GatherBytes(Sse2.ConvertToVector128Int32(color.v).AsUInt32());

				//Although less accurate, this way of approximating the square root correctly handles zero (unlike x * 1/sqrt x)
				static Float4 ApproximateSqrt(in Float4 value) => new(Sse.Reciprocal(Sse.ReciprocalSqrt(value.v)));
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