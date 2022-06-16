using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Common;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Operations;
using Echo.Core.Textures.Evaluation;
using Echo.Core.Textures.Grid;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;
using SDL2;

namespace Echo.UserInterface.Core.Areas;

using static SDL;

public class TilesUI : PlaneUI
{
	public TilesUI() : base("Tiles") { }

	IntPtr texture;
	Int2 textureSize;
	Float2 textureExtend;

	Procedure[] procedures;

	EvaluationOperation lastOperation;
	readonly Queue<uint> indexQueue = new();
	uint nextExploreIndex;

	protected override void Update(in Moment moment)
	{
		if (Device.Instance?.LatestOperation is not EvaluationOperation { destination: { } layer } operation) return;

		//Check if operation or buffer changed
		Int2 layerSize = ((TextureGrid)layer).size;

		if (layerSize != textureSize)
		{
			if (texture != IntPtr.Zero) Root.Backend.DestroyTexture(ref texture);
			texture = Root.Backend.CreateTexture(textureSize = layerSize, true);
		}

		if (operation != lastOperation)
		{
			RestartTextureUpdate();
			lastOperation = operation;
		}

		//Find and update tile from completed procedures
		IEvaluationReadTile tile;
		do
		{
			tile = RequestUpdateTile(operation);
			if (tile != null) UpdateTexture(tile);
		}
		while (tile != null);

		//Draw all
		DrawHeader(layer);
		base.Update(moment);
	}

	protected override void Draw(ImDrawListPtr drawList, in Bounds region)
	{
		FindContentBounds(region, out Bounds content);

		drawList.AddImage(texture, content.MinVector2, content.MaxVector2);

		DrawCurrentTiles(lastOperation, drawList, content);
	}

	protected override void Dispose(bool disposing)
	{
		ReleaseUnmanagedResources();
		base.Dispose(disposing);
	}

	void RestartTextureUpdate()
	{
		//Clear queue
		indexQueue.Clear();
		nextExploreIndex = 0;

		//Clear texture
		SDL_LockTexture(texture, IntPtr.Zero, out IntPtr pointer, out int pitch).ThrowOnError();

		unsafe
		{
			if (pitch == sizeof(uint) * textureSize.X) new Span<uint>((uint*)pointer, textureSize.Product).Fill(0xFF1F1511u);
			else throw new InvalidOperationException("Fill assumption with contiguous memory from SDL2 backend is violated!");
		}

		SDL_UnlockTexture(texture);
	}

	IEvaluationReadTile RequestUpdateTile(EvaluationOperation operation)
	{
		//Enqueue indices to explore
		while (indexQueue.Count < operation.WorkerCount && nextExploreIndex < operation.totalProcedureCount)
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

	unsafe void UpdateTexture(IEvaluationReadTile tile)
	{
		Int2 tileSize = tile.Size;
		if (tileSize == Int2.Zero) return;

		//Grab tile bounds to pixel destination
		Int2 min = tile.Min;
		Int2 max = tile.Max;

		int invertY = textureSize.Y - min.Y - tileSize.Y;

		SDL_Rect rect = new SDL_Rect { x = min.X, y = invertY, w = tileSize.X, h = tileSize.Y };
		SDL_LockTexture(texture, ref rect, out IntPtr pointer, out int pitch).ThrowOnError();

		int stride = pitch / sizeof(uint);
		uint* pixels = (uint*)pointer - min.X + stride * (tileSize.Y - 1);

		//Convert and assign each pixel
		for (int y = min.Y; y < max.Y; y++)
		{
			for (int x = min.X; x < max.X; x++)
			{
				Float4 color = tile[new Int2(x, y)];
				color = ApproximateSqrt(color.Max(Float4.Zero));
				color = color.Min(Float4.One) * byte.MaxValue;

				// 000000WW 000000ZZ 000000YY 000000XX               original
				//       00 0000WW00 0000ZZ00 0000YY000000XX         shift by 3 bytes
				// 000000WW 0000WWZZ 0000ZZYY 0000YYXX               or with original
				//              0000 00WW0000 WWZZ0000ZZYY0000YYXX   shift by 6 bytes
				// 000000WW 0000WWZZ 00WWZZYY WWZZYYXX               or with original

				Vector128<uint> pixel = Sse2.ConvertToVector128Int32(color.v).AsUInt32();
				pixel = Sse2.Or(pixel, Sse2.ShiftRightLogical128BitLane(pixel, 3));
				pixel = Sse2.Or(pixel, Sse2.ShiftRightLogical128BitLane(pixel, 6));

				pixels[x] = pixel.ToScalar();

				static Float4 ApproximateSqrt(in Float4 value) => new(Sse.Reciprocal(Sse.ReciprocalSqrt(value.v)));
			}

			pixels -= stride;
		}

		SDL_UnlockTexture(texture);
	}

	void DrawHeader(IEvaluationLayer layer)
	{
		if (ImGui.Button("Save Buffer to File"))
		{
			ActionQueue.Enqueue(Serialize, "Serialize Evaluation Layer");
			void Serialize() => ((TextureGrid)layer).Save("render.png");
		}

		ImGui.SameLine();
		if (ImGui.Button("Compare with File")) CompareWithFile(layer);

		ImGui.SameLine();
		if (ImGui.Button("Show Working Directory"))
		{
			string path = Environment.CurrentDirectory;

			if (!Path.EndsInDirectorySeparator(path)) path += Path.DirectorySeparatorChar;
			Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
		}

		ImGui.SameLine();
		if (ImGui.Button("Recenter View")) Recenter();

		ImGui.SameLine();
		if (ImGui.Button("Refresh Tiles")) RestartTextureUpdate();

		ImGui.SameLine();
		DrawMousePixelInformation(layer);
	}

	/// <summary>
	/// Finds the <see cref="PlaneUI.Bounds"/> for the display content.
	/// </summary>
	void FindContentBounds(in Bounds region, out Bounds content)
	{
		float aspect = (float)textureSize.X / textureSize.Y;
		float regionAspect = region.extend.X / region.extend.Y;

		if (aspect > regionAspect)
		{
			float width = region.extend.X;
			textureExtend = new Float2(width, width / aspect);
		}
		else
		{
			float height = region.extend.Y;
			textureExtend = new Float2(height * aspect, height);
		}

		content = new Bounds(region.center + PlaneCenter, textureExtend * PlaneScale);
	}

	void DrawCurrentTiles(EvaluationOperation operation, ImDrawListPtr drawList, in Bounds content)
	{
		IEvaluationLayer layer = operation.destination;
		Float2 sizeR = ((TextureGrid)layer).sizeR;

		Float2 invertMin = new Float2(content.min.X, content.max.Y);
		Float2 invertMax = new Float2(content.max.X, content.min.Y);

		uint progressColor = GetColorFromStyle(ImGuiCol.FrameBg);
		uint borderColor = GetColorFromStyle(ImGuiCol.CheckMark);

		foreach (ref readonly Procedure procedure in GatherValidProcedures(operation))
		{
			Int2 tilePosition = operation.tilePositions[(int)procedure.index];
			layer.GetTileBounds(tilePosition, out Int2 tileMin, out Int2 tileMax);

			Float2 min = Float2.Lerp(invertMin, invertMax, tileMin * sizeR);
			Float2 max = Float2.Lerp(invertMin, invertMax, tileMax * sizeR);

			float progress = (float)procedure.Progress;
			Float2 height = new(max.X, Scalars.Lerp(min.Y, max.Y, progress));
			drawList.AddRectFilled(min.AsVector2(), height.AsVector2(), progressColor);
			drawList.AddRect(min.AsVector2(), max.AsVector2(), borderColor);
		}
	}

	void DrawMousePixelInformation(IEvaluationLayer layer)
	{
		Float2 cursorPosition = CursorPosition ?? Float2.NegativeInfinity;
		cursorPosition = cursorPosition / textureExtend / 2f + Float2.Half;
		cursorPosition = new Float2(1f - cursorPosition.X, cursorPosition.Y);

		if (Float2.Zero <= cursorPosition && cursorPosition < Float2.One)
		{
			Int2 position = (Int2)(cursorPosition * textureSize);
			Int2 tilePosition = layer.GetTilePosition(position);
			IEvaluationReadTile tile = layer.RequestTile(tilePosition);

			ImGui.TextUnformatted(tile == null ?
				$"Pixel: {position} Tile: {tilePosition}" :
				$"Pixel: {position} Tile: {tilePosition} RGBA: {tile[position]:N4}");
		}
		else ImGui.TextUnformatted("Mouse Content Unavailable");
	}

	ReadOnlySpan<Procedure> GatherValidProcedures(Operation operation)
	{
		Utility.EnsureCapacity(ref procedures, operation.WorkerCount);

		//Find the filter through the procedures
		SpanFill<Procedure> fill = procedures;
		SpanFill<Procedure> result = procedures;
		operation.FillWorkerProcedures(ref fill);

		foreach (ref readonly Procedure procedure in fill.Filled)
		{
			if (procedure.Progress > 0d || procedure.index != 0) result.Add(procedure);
		}

		return result.Filled;
	}

	void ReleaseUnmanagedResources() => Root.Backend.DestroyTexture(ref texture);

	~TilesUI() => Dispose(false);

	static uint GetColorFromStyle(ImGuiCol styleColor)
	{
		var style = ImGui.GetStyle();

		Vector4 c = style.Colors[(int)styleColor];
		var color = new Color32(c.X, c.Y, c.Z, c.W);
		return Unsafe.As<Color32, uint>(ref color);
	}

	static void CompareWithFile(IEvaluationLayer layer)
	{
		var device = Device.Instance;
		if (device == null) return;

		ActionQueue.Enqueue(Dispatch, "Compare Operation Dispatch");

		void Dispatch()
		{
			// device.Dispatch(operation);
		}
	}
}