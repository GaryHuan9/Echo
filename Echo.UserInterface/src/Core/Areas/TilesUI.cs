using System;
using System.Collections.Generic;
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

public class TilesUI : AreaUI
{
	public TilesUI() : base("Tiles") { }

	IntPtr texture;
	Int2 textureSize;

	Procedure[] procedures;

	EvaluationOperation lastOperation;
	readonly Queue<uint> indexQueue = new();
	uint nextExploreIndex;

	float displayScale;
	Float2 displayCenter;

	protected override void Update(in Moment moment)
	{
		if (Device.Instance?.LatestOperation is not EvaluationOperation { destination: { } } operation) return;

		//Check if operation or buffer changed
		Int2 layerSize = ((TextureGrid)operation.destination).size;

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

		//Draw everything
		Draw(operation);
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

	void Draw(EvaluationOperation operation)
	{
		IEvaluationLayer layer = operation.destination;

		if (ImGui.Button("Save Buffer"))
		{
			ActionQueue.Enqueue(Serialize, "Serialize Evaluation Layer");
			void Serialize() => ((TextureGrid)layer).Save("render.png");
		}

		ImGui.SameLine();
		if (ImGui.Button("Recenter View"))
		{
			displayScale = 0f;
			displayCenter = Float2.Zero;
		}

		ImGui.SameLine();
		if (ImGui.Button("Refresh Tiles")) RestartTextureUpdate();

		FindBounds(out Bounds region, out Bounds content);
		ImGui.SameLine();
		DrawMousePixelInformation(layer, region, content);

		var drawList = ImGui.GetWindowDrawList();

		drawList.PushClipRect(region.MinVector2, region.MaxVector2);
		drawList.AddImage(texture, content.MinVector2, content.MaxVector2);

		DrawCurrentTiles(operation, drawList, content);

		drawList.PopClipRect();
		ProcessMouseInput(region);
	}

	void FindBounds(out Bounds region, out Bounds content)
	{
		//Find bounds for all available space
		Float2 min = ImGui.GetCursorScreenPos().AsFloat2();
		Float2 max = min + ImGui.GetContentRegionAvail().AsFloat2();
		region = new Bounds((max + min) / 2f, (max - min) / 2f);

		//Find display content bounds
		float aspect = (float)textureSize.X / textureSize.Y;
		float regionAspect = region.extend.X / region.extend.Y;
		Float2 extend;

		if (aspect > regionAspect)
		{
			float width = region.extend.X;
			extend = new Float2(width, width / aspect);
		}
		else
		{
			float height = region.extend.Y;
			extend = new Float2(height * aspect, height);
		}

		content = new Bounds(region.center + displayCenter, extend * MathF.Exp(displayScale));
	}

	void DrawMousePixelInformation(IEvaluationLayer layer, in Bounds region, in Bounds content)
	{
		if (!TryGetMousePixelPosition(layer, region, content, out Int2 position))
		{
			ImGui.TextUnformatted("Mouse Content Unavailable");
			return;
		}

		Int2 tilePosition = layer.GetTilePosition(position);
		IEvaluationReadTile tile = layer.RequestTile(tilePosition);

		if (tile == null)
		{
			ImGui.TextUnformatted($"Pixel: {position} Tile: {tilePosition}");
			return;
		}

		ImGui.TextUnformatted($"Pixel: {position} Tile: {tilePosition} RGBA: {tile[position]:N4}");
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

	void ProcessMouseInput(in Bounds region)
	{
		//Detect for interaction
		ImGui.SetCursorScreenPos(region.MinVector2);
		ImGui.PushAllowKeyboardFocus(false);
		ImGui.InvisibleButton("Display", region.extend.AsVector2() * 2f);
		ImGui.PopAllowKeyboardFocus();

		if (!ImGui.IsItemActive() && !ImGui.IsItemHovered()) return;

		var io = ImGui.GetIO();
		float zoom = io.MouseWheel * 0.1f;

		//Move if the left mouse button is held 
		if (io.MouseDown[0]) displayCenter += io.MouseDelta.AsFloat2();
		if (zoom == 0) return;

		//Math to zoom towards cursor
		Float2 offset = region.center - io.MousePos.AsFloat2();
		Float2 point = (offset + displayCenter) / MathF.Exp(displayScale);
		displayCenter = point * MathF.Exp(displayScale += zoom) - offset;
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

	static bool TryGetMousePixelPosition(IEvaluationLayer layer, in Bounds region, in Bounds content, out Int2 position)
	{
		position = default;
		if (!ImGui.IsMouseHoveringRect(region.MinVector2, region.MaxVector2, false)) return false;

		Float2 mouse = ImGui.GetMousePos().AsFloat2();
		Float2 percent = Float2.InverseLerp(content.min, content.max, mouse);
		if (!(Float2.Zero <= percent) || !(percent <= Float2.One)) return false;

		position = (Int2)(((TextureGrid)layer).size * new Float2(percent.X, 1f - percent.Y));
		return true;
	}

	readonly struct Bounds
	{
		public Bounds(Float2 center, Float2 extend)
		{
			Assert.IsTrue(extend >= Float2.Zero);

			this.center = center;
			this.extend = extend;

			min = center - extend;
			max = center + extend;
		}

		public readonly Float2 center;
		public readonly Float2 extend;

		public readonly Float2 min;
		public readonly Float2 max;

		public Vector2 MinVector2 => min.AsVector2();
		public Vector2 MaxVector2 => max.AsVector2();
	}
}