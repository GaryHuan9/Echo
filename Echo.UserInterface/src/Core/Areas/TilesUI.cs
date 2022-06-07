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
using Echo.Core.Textures.Grid;
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

	uint nextProcedureIndex;
	TimeSpan totalElapsed;

	readonly TimeSpan regularUpdateDelay = TimeSpan.FromSeconds(1f / 100f);
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
			ClearTexture();
		}

		//Update and display texture
		totalElapsed += moment.delta;
		ConsumeElapsedTime(operation);

		// ImGui.SliderFloat("Label0", )
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
		uint count = operation.CompletedProcedureCount;
		while (nextProcedureIndex < count)
		{
			UpdateTexture(operation, nextProcedureIndex++);
			if (--updateCount == 0) return;
		}

		//Update random times from procedures that are currently being processed
		var candidates = GatherValidProcedures(operation);
		updateCount = Math.Min(updateCount, candidates.Length);

		for (int i = 0; i < updateCount; i++)
		{
			UpdateTexture(operation, candidates[i].index);
		}
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

		RenderBuffer buffer = operation.profile.Buffer;
		int invertY = buffer.size.Y - min.Y - size.Y;

		SDL_Rect rect = new SDL_Rect { x = min.X, y = invertY, w = size.X, h = size.Y };
		SDL_LockTexture(texture, ref rect, out IntPtr pointer, out int pitch).ThrowOnError();

		int stride = pitch / sizeof(uint);
		uint* pixels = (uint*)pointer - min.X + stride * size.Y - stride;

		for (int y = min.Y; y < max.Y; y++)
		{
			for (int x = min.X; x < max.X; x++)
			{
				Float4 color = (RGBA128)buffer[new Int2(x, y)];
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
			}

			pixels -= stride;
		}

		SDL_UnlockTexture(texture);

		static Float4 ApproximateSqrt(in Float4 value) => new(Sse.Reciprocal(Sse.ReciprocalSqrt(value.v)));
	}

	unsafe void ClearTexture()
	{
		SDL_LockTexture(texture, IntPtr.Zero, out IntPtr pointer, out int pitch).ThrowOnError();
		if (pitch == sizeof(uint) * textureSize.X) new Span<uint>((uint*)pointer, textureSize.Product).Fill(0xFF000000u);
		else throw new InvalidOperationException("Fill assumption with contiguous memory from SDL2 backend is violated!");

		SDL_UnlockTexture(texture);
	}

	void ReleaseUnmanagedResources() => Root.Backend.DestroyTexture(ref texture);

	~TilesUI() => Dispose(false);
}