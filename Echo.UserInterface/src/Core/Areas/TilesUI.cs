using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Echo.Core.Common;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Operation;
using Echo.Core.InOut;
using Echo.Core.Textures.Evaluation;
using Echo.Core.Textures.Grids;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;
using SDL2;

namespace Echo.UserInterface.Core.Areas;

using static SDL;

public class TilesUI : PlaneUI
{
	public TilesUI() : base("Tiles") { }

	public override void Initialize()
	{
		base.Initialize();
		operationUI = Root.Find<OperationUI>();
	}

	OperationUI operationUI;
	Procedure[] procedures;

	IntPtr texture;
	Int2 textureSize;
	Float2 textureExtend;
	TextureGrid compareTexture;

	EvaluationOperation lastOperation;
	readonly Queue<uint> indexQueue = new();
	uint nextExploreIndex;

	static ReadOnlySpan<string> ImagePaths => new[] { "render.fpi", "render.png" };

	protected override bool HasMenuBar => true;

	protected override void Update(in Moment moment)
	{
		if (operationUI.SelectedOperation is not EvaluationOperation { destination: { } layer } operation) return;

		//Check if operation or buffer changed
		Int2 layerSize = ((TextureGrid)layer).size;

		if (layerSize != textureSize)
		{
			textureSize = layerSize;
			Root.Backend.DestroyTexture(ref texture);
			texture = Root.Backend.CreateTexture(layerSize, true);
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
		DrawMenuBar(layer);
		base.Update(moment);
	}

	protected override void Draw(ImDrawListPtr drawList, in Bounds region)
	{
		FindContentBounds(region, out Bounds content);

		uint borderColor = ImGuiCustom.GetColorInteger(ImGuiCol.Border);
		drawList.AddRectFilled(content.MinVector2, content.MaxVector2, 0xFF000000u);
		drawList.AddImage(texture, content.MinVector2, content.MaxVector2);
		drawList.AddRect(content.MinVector2, content.MaxVector2, borderColor);

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
		SDL_SetTextureBlendMode(texture, SDL_BlendMode.SDL_BLENDMODE_BLEND).ThrowOnError();

		unsafe
		{
			if (pitch == sizeof(uint) * textureSize.X) new Span<uint>((uint*)pointer, textureSize.Product).Clear();
			else throw new InvalidOperationException("Fill assumption of contiguous memory from SDL2 is violated!");
		}

		SDL_UnlockTexture(texture);
	}

	IEvaluationReadTile RequestUpdateTile(EvaluationOperation operation)
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

	unsafe void UpdateTexture(IEvaluationReadTile tile)
	{
		Int2 tileSize = tile.Size;
		if (tileSize == Int2.Zero) return;

		//Grab tile bounds to pixel destination
		Int2 min = tile.Min;
		Int2 max = tile.Max;

		int invertY = textureSize.Y - min.Y - tileSize.Y;
		Float2 textureSizeR = 1f / textureSize;

		SDL_Rect rect = new SDL_Rect { x = min.X, y = invertY, w = tileSize.X, h = tileSize.Y };
		SDL_LockTexture(texture, ref rect, out IntPtr pointer, out int pitch).ThrowOnError();

		int stride = pitch / sizeof(uint);
		uint* pixels = (uint*)pointer - min.X + stride * (tileSize.Y - 1);

		//Convert and assign each pixel
		for (int y = min.Y; y < max.Y; y++, pixels -= stride)
		for (int x = min.X; x < max.X; x++)
		{
			Int2 position = new Int2(x, y);
			Float4 color = tile[position];

			if (compareTexture != null)
			{
				Float2 uv = (position + Float2.Half) * textureSizeR;
				Float4 reference = compareTexture[uv];
				color = Float4.Half + color - reference;
				color += Float4.Ana; //Force alpha to be 1
			}

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

		SDL_UnlockTexture(texture);
	}

	void DrawMenuBar(IEvaluationLayer layer)
	{
		if (ImGui.BeginMenuBar())
		{
			if (ImGui.BeginMenu("File"))
			{
				if (ImGui.MenuItem("Save to Disk"))
				{
					ActionQueue.Enqueue("Serialize Evaluation Layer", () =>
					{
						foreach (string path in ImagePaths) ((TextureGrid)layer).Save(path);
					});
				}

				if (ImGui.MenuItem("Show Directory"))
				{
					string path = Environment.CurrentDirectory;

					if (!Path.EndsInDirectorySeparator(path)) path += Path.DirectorySeparatorChar;
					Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
				}

				bool isComparing = compareTexture != null;

				if (ImGui.MenuItem(isComparing ? "End Comparison" : "Compare with File"))
				{
					compareTexture = null;

					if (!isComparing)
					{
						foreach (string path in ImagePaths)
						{
							if (!File.Exists(path)) continue;
							compareTexture = ((TextureGrid)layer).Load(path);
							break;
						}

						if (compareTexture == null) LogList.AddError($"Unable to find reference texture file at '{Environment.CurrentDirectory}'.");
					}

					RestartTextureUpdate();
				}

				if (isComparing && ImGui.MenuItem("Print Average"))
				{
					Float2 sizeR = 1f / textureSize;
					Summation total = Summation.Zero;

					for (int y = 0; y < textureSize.Y; y++)
					for (int x = 0; x < textureSize.X; x++)
					{
						//We use uv to index both textures because the integer indexer is only available to typed textures
						//This is a little bit dumb but it works for now and this is just a temporary handy tool

						Float2 uv = new Float2(x + 0.5f, y + 0.5f) * sizeR;
						Float4 color = ((TextureGrid)layer)[uv];
						total += color - compareTexture![uv];
					}

					Float4 average = total.Result / textureSize.Product;
					LogList.Add($"Average color difference versus reference: {average:N4}.");
				}

				ImGui.EndMenu();
			}

			if (ImGui.BeginMenu("View"))
			{
				if (ImGui.MenuItem("Recenter")) Recenter();
				if (ImGui.MenuItem("Refresh")) RestartTextureUpdate();

				ImGui.EndMenu();
			}

			ImGui.EndMenuBar();
		}

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

		uint fillColor = ImGuiCustom.GetColorInteger(ImGuiCol.MenuBarBg);
		uint borderColor = ImGuiCustom.GetColorInteger();

		foreach (ref readonly Procedure procedure in GatherValidProcedures(operation))
		{
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

	void DrawMousePixelInformation(IEvaluationLayer layer)
	{
		Float2 cursorPosition = CursorPosition ?? Float2.NegativeInfinity;
		Float2 uv = cursorPosition / textureExtend;
		uv = new Float2(-uv.X, uv.Y) / 2f + Float2.Half;

		if (!(Float2.Zero <= uv) || !(uv < Float2.One))
		{
			ImGui.NewLine();
			return;
		}

		Int2 position = (Int2)(uv * textureSize);
		Int2 tilePosition = layer.GetTilePosition(position);
		IEvaluationReadTile tile = layer.RequestTile(tilePosition);

		if (tile != null)
		{
			Float4 color = tile[position];

			if (compareTexture != null)
			{
				uv = (position + Float2.Half) / textureSize;
				color = (color - compareTexture[uv]).Absoluted;
			}

			ImGui.TextUnformatted($"Pixel: {position.ToInvariant()} Tile: {tilePosition.ToInvariant()} RGBA: {color.ToInvariant()}");
		}
		else ImGui.TextUnformatted($"Pixel: {position.ToInvariant()} Tile: {tilePosition.ToInvariant()}");
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
}