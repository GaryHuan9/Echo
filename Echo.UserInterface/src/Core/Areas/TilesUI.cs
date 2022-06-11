using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Common;
using Echo.Common.Compute;
using Echo.Common.Memory;
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

	protected override void UpdateImpl(in Moment moment)
	{
		if (Device.Instance?.LatestOperation is not EvaluationOperation { profile.Buffer: { } buffer } operation) return;

		//Check if operation or buffer changed
		if (buffer.size != textureSize)
		{
			if (texture != IntPtr.Zero) Root.Backend.DestroyTexture(ref texture);
			texture = Root.Backend.CreateTexture(textureSize = buffer.size, true);
		}

		if (operation != lastOperation)
		{
			RecenterView();
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

	void RecenterView()
	{
		displayScale = 0f;
		displayCenter = Float2.Zero;
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

	void SaveRenderBuffer(EvaluationOperation operation)
	{
		TextureGrid buffer = (TextureGrid)operation.destination;
		ActionQueue.Enqueue(Serialize, "Save Evaluation Layer");

		void Serialize() => buffer.Save("render.png");
	}

	void Draw(EvaluationOperation operation)
	{
		if (ImGui.Button("Save Buffer")) SaveRenderBuffer(operation);

		ImGui.SameLine();
		if (ImGui.Button("Recenter View")) RecenterView();

		ImGui.SameLine();
		if (ImGui.Button("Refresh Tiles")) RestartTextureUpdate();

		FindCurrentRegionBounds(out Bounds region);
		CalculateContentBounds(region, out Bounds content);

		ImGui.SameLine();
		Int2? position = TryGetMousePixelPosition(operation, region, content);
		if (position != null)
		{
			ImGui.TextUnformatted(position.ToString());
		}
		else ImGui.TextUnformatted("Pixel Content Unavailable");

		var drawList = ImGui.GetWindowDrawList();

		drawList.PushClipRect(region.MinVector2, region.MaxVector2);
		drawList.AddImage(texture, content.MinVector2, content.MaxVector2);

		DrawCurrentTiles(operation, drawList, content);

		drawList.PopClipRect();
		ProcessMouseInput(region);
	}

	void FindCurrentRegionBounds(out Bounds region)
	{
		Float2 min = ImGui.GetCursorScreenPos().AsFloat2();
		Float2 max = min + ImGui.GetContentRegionAvail().AsFloat2();
		region = new Bounds((max + min) / 2f, (max - min) / 2f);
	}

	void CalculateContentBounds(in Bounds region, out Bounds content)
	{
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

	Int2? TryGetMousePixelPosition(EvaluationOperation operation, in Bounds region, in Bounds content)
	{
		if (!ImGui.IsMouseHoveringRect(region.MinVector2, region.MaxVector2, false)) return null;

		Float2 mouse = ImGui.GetMousePos().AsFloat2();
		Float2 size = operation.profile.Buffer.size;

		Float2 percent = Float2.InverseLerp(content.Min, content.Max, mouse);
		if (!(Float2.Zero <= percent) || !(percent <= Float2.One)) return null;
		return (Int2)(size * percent);
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

	void DrawCurrentTiles(EvaluationOperation operation, ImDrawListPtr drawList, in Bounds content)
	{
		Float2 sizeR = operation.profile.Buffer.sizeR;
		IEvaluationLayer layer = operation.destination;

		Float2 invertMin = new Float2(content.Min.X, content.Max.Y);
		Float2 invertMax = new Float2(content.Max.X, content.Min.Y);

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

	void ReleaseUnmanagedResources() => Root.Backend.DestroyTexture(ref texture);

	~TilesUI() => Dispose(false);

	static uint GetColorFromStyle(ImGuiCol styleColor)
	{
		var style = ImGui.GetStyle();

		Vector4 c = style.Colors[(int)styleColor];
		var color = new Color32(c.X, c.Y, c.Z, c.W);
		return Unsafe.As<Color32, uint>(ref color);
	}

	readonly struct Bounds
	{
		public Bounds(Float2 center, Float2 extend)
		{
			this.center = center;
			this.extend = extend;
		}

		public readonly Float2 center;
		public readonly Float2 extend;

		public Float2 Min => center - extend;
		public Float2 Max => center + extend;

		public Vector2 MinVector2 => Min.AsVector2();
		public Vector2 MaxVector2 => Max.AsVector2();
	}
}