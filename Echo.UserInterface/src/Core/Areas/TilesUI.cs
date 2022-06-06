using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Packed;
using Echo.Common;
using Echo.Common.Compute;
using Echo.Common.Mathematics.Randomization;
using Echo.Common.Memory;
using Echo.Core.Evaluation.Operations;
using Echo.Core.Textures.Colors;
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

	Procedure[] procedures = Array.Empty<Procedure>();

	TiledEvaluationOperation lastOperation;

	uint nextProcedureIndex;
	TimeSpan totalElapsed;

	readonly TimeSpan regularUpdateDelay = TimeSpan.FromSeconds(1f / 6f);
	readonly TimeSpan boostedUpdateDelay = TimeSpan.FromSeconds(1f / 100f);

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
			nextProcedureIndex = 0;
			lastOperation = operation;

			//TODO: clear texture
		}

		//Update and display texture
		totalElapsed += moment.delta;
		ConsumeElapsedTime(operation);

		ImGui.Image(texture, new Vector2(textureSize.X, textureSize.Y));
	}

	protected override void Dispose(bool disposing)
	{
		ReleaseUnmanagedResources();
		base.Dispose(disposing);
	}

	void ConsumeElapsedTime(TiledEvaluationOperation operation)
	{
		//Find the total number of updates
		TimeSpan delay = operation.IsCompleted ? boostedUpdateDelay : regularUpdateDelay;
		int updateCount = 0;

		while (totalElapsed >= delay)
		{
			totalElapsed -= delay;
			++updateCount;
		}

		if (updateCount == 0) return;

		//Update tiles from completed procedures
		for (; nextProcedureIndex < operation.CompletedProcedureCount; nextProcedureIndex++)
		{
			UpdateTexture(operation, nextProcedureIndex);
			if (--updateCount == 0) return;
		}

		//Update random times from procedures that are currently being processed
		var candidates = GatherValidProcedures(operation);

		for (int i = 0; i < updateCount; i++)
		{
			UpdateTexture(operation, candidates[i].index);
		}

		SDL_UnlockTexture(texture);
	}

	ReadOnlySpan<Procedure> GatherValidProcedures(Operation operation)
	{
		Utilities.EnsureCapacity(ref procedures, operation.WorkerCount);

		//Find the filter through the procedures
		SpanFill<Procedure> fill = procedures;
		SpanFill<Procedure> result = procedures;
		operation.FillWorkerProcedures(ref fill);

		foreach (ref readonly Procedure procedure in fill.Filled)
		{
			if (procedure.Progress > 0d) result.Add(procedure);
		}

		//Shuffle so we randomly use them
		Span<Procedure> span = result.Filled;
		SystemPrng.Shared.Shuffle(span);
		return span;
	}

	unsafe void UpdateTexture(TiledEvaluationOperation operation, uint procedureIndex)
	{
		operation.GetTileMinMax(procedureIndex, out Int2 min, out Int2 max);

		Int2 size = max - min;
		if (size == Int2.Zero) return;
		var buffer = operation.profile.Buffer;

		SDL_Rect rect = new SDL_Rect { x = min.X, y = min.Y, w = size.X, h = size.Y };
		SDL_LockTexture(texture, ref rect, out IntPtr pointer, out int pitch).ThrowOnError();

		uint* pixels = (uint*)pointer - min.X;
		int stride = pitch / sizeof(uint);

		for (int y = min.Y; y < max.Y; y++)
		{
			for (int x = min.X; x < max.X; x++)
			{
				Float4 color = (Float4)(RGBA128)buffer[new Int2(x, y)] * byte.MaxValue;
				Vector128<uint> pixel = Sse2.ConvertToVector128Int32(color.v).AsUInt32();

				// 000000WW 000000ZZ 000000YY 000000XX               original
				//       00 0000WW00 0000ZZ00 0000YY000000XX         shift by 3 bytes
				// 000000WW 0000WWZZ 0000ZZYY 0000YYXX               or with original
				//              0000 00WW0000 WWZZ0000ZZYY0000YYXX   shift by 6 bytes
				// 000000WW 0000WWZZ 00WWZZYY WWZZYYXX               or with original

				pixel = Sse2.Or(pixel, Sse2.ShiftRightLogical128BitLane(pixel, 3));
				pixel = Sse2.Or(pixel, Sse2.ShiftRightLogical128BitLane(pixel, 6));

				pixels[x] = pixel.ToScalar();
			}

			pixels += stride;
		}
	}

	void ReleaseUnmanagedResources() => Root.Backend.DestroyTexture(ref texture);

	~TilesUI() => Dispose(false);
}