using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Packed;
using Echo.Common;
using Echo.Common.Compute;
using Echo.Common.Memory;
using Echo.Core.Evaluation.Operations;
using Echo.Core.Textures.Evaluation;
using Echo.UserInterface.Backend;
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

	TiledEvaluationOperation lastOperation;
	readonly Queue<uint> indexQueue = new();
	uint nextExploreIndex;

	float displayScale;
	Vector2 displayCenter;

	protected override void Draw(in Moment moment)
	{
		if (Device.Instance?.LatestOperation is not TiledEvaluationOperation { profile.Buffer: { } buffer } operation) return;

		//Check if operation or buffer changed
		if (buffer.size != textureSize)
		{
			if (texture != IntPtr.Zero) Root.Backend.DestroyTexture(ref texture);
			texture = Root.Backend.CreateTexture(textureSize = buffer.size, true);
		}

		if (operation != lastOperation)
		{
			ClearTexture();
			indexQueue.Clear();
			nextExploreIndex = 0;

			displayScale = 0f;
			displayCenter = Vector2.Zero;
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
		BeginUpdateDisplay(out ImDrawListPtr drawList, out Vector2 center, out Vector2 extend);

		drawList.AddImage(texture, center - extend, center + extend);

		// foreach (ref readonly Procedure procedure in GatherValidProcedures(operation)) { }

		drawList.PopClipRect();
	}

	protected override void Dispose(bool disposing)
	{
		ReleaseUnmanagedResources();
		base.Dispose(disposing);
	}

	unsafe void ClearTexture()
	{
		SDL_LockTexture(texture, IntPtr.Zero, out IntPtr pointer, out int pitch).ThrowOnError();

		if (pitch == sizeof(uint) * textureSize.X) new Span<uint>((uint*)pointer, textureSize.Product).Fill(0xFF1F1511u);
		else throw new InvalidOperationException("Fill assumption with contiguous memory from SDL2 backend is violated!");

		SDL_UnlockTexture(texture);
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
			if (procedure.Progress > 0d) result.Add(procedure);
		}

		return result.Filled;
	}

	IEvaluationReadTile RequestUpdateTile(TiledEvaluationOperation operation)
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

	void BeginUpdateDisplay(out ImDrawListPtr drawList, out Vector2 center, out Vector2 extend)
	{
		//Find available content space
		Vector2 contentMin = ImGui.GetCursorScreenPos();
		Vector2 contentMax = contentMin + ImGui.GetContentRegionAvail();

		Vector2 contentSize = contentMax - contentMin;
		Vector2 contentCenter = (contentMax + contentMin) / 2f;

		//Orient display from input
		ImGui.SetCursorScreenPos(contentMin);
		ImGui.PushAllowKeyboardFocus(false);
		ImGui.InvisibleButton("Display", contentSize);
		ImGui.PopAllowKeyboardFocus();

		if (ImGui.IsItemActive() || ImGui.IsItemHovered()) OrientDisplay(contentCenter);

		//Calculate correct size
		float aspect = (float)textureSize.X / textureSize.Y;
		float contentAspect = contentSize.X / contentSize.Y;
		Vector2 size;

		if (aspect > contentAspect)
		{
			float width = contentSize.X;
			size = new Vector2(width, width / aspect);
		}
		else
		{
			float height = contentSize.Y;
			size = new Vector2(height * aspect, height);
		}

		//Output orientation and begin draw list
		center = contentCenter + displayCenter;
		extend = size / 2f * MathF.Exp(displayScale);

		drawList = ImGui.GetWindowDrawList();
		drawList.PushClipRect(contentMin, contentMax);
	}

	void OrientDisplay(Vector2 contentCenter)
	{
		var io = ImGui.GetIO();
		float zoom = io.MouseWheel * 0.1f;

		//Move if the left mouse button is held 
		if (io.MouseDown[0]) displayCenter += io.MouseDelta;
		if (zoom == 0) return;

		//Math to zoom towards cursor
		Vector2 offset = contentCenter - io.MousePos;
		Vector2 point = (offset + displayCenter) / MathF.Exp(displayScale);
		displayCenter = point * MathF.Exp(displayScale += zoom) - offset;
	}

	void ReleaseUnmanagedResources() => Root.Backend.DestroyTexture(ref texture);

	~TilesUI() => Dispose(false);
}