using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grid;

namespace Echo.Core.Textures.Evaluation;

public interface IEvaluationLayer
{
	IEvaluationWriteTile CreateTile(Int2 tilePosition);

	IEvaluationReadTile RequestTile(Int2 tilePosition);

	void Write(IEvaluationWriteTile tile);
}

public class EvaluationLayer<T> : TextureGrid<T>, IEvaluationLayer where T : unmanaged, IColor<T>
{
	public EvaluationLayer(Int2 size, Int2 tileSize) : base(size)
	{
		if (!BitOperations.IsPow2(tileSize.X) || !BitOperations.IsPow2(tileSize.Y)) throw new ArgumentOutOfRangeException(nameof(tileSize));

		this.tileSize = tileSize;
		tileRange = size / tileSize;

		logTileSize = new Int2
		(
			BitOperations.Log2((uint)tileSize.X),
			BitOperations.Log2((uint)tileSize.Y)
		);

		tileBuffers = new T[tileRange.Product][];
	}

	public readonly Int2 tileSize;
	public readonly Int2 tileRange;

	readonly Int2 logTileSize;
	readonly T[][] tileBuffers;

	public override T this[Int2 position]
	{
		get
		{
			Int2 tilePosition = GetTilePosition(position);
			int tileIndex = GetTileIndex(tilePosition);
			T[] buffer = tileBuffers[tileIndex];
			if (buffer == null) return default;

			GetTileBounds(tilePosition, out Int2 min, out Int2 max);
			return buffer[GetLocalOffset(position, min, max)];
		}
	}

	/// <inheritdoc/>
	public IEvaluationWriteTile CreateTile(Int2 tilePosition)
	{
		AssertValidPosition(tilePosition, tileRange);
		GetTileBounds(tilePosition, out Int2 min, out Int2 max);
		return new WriteTile(tilePosition, min, max);
	}

	/// <inheritdoc/>
	public IEvaluationReadTile RequestTile(Int2 tilePosition)
	{
		AssertValidPosition(tilePosition, tileRange);
		var buffer = tileBuffers[GetTileIndex(tilePosition)];
		if (buffer == null) return null;

		GetTileBounds(tilePosition, out Int2 min, out Int2 max);
		return new ReadTile(tilePosition, min, max, buffer);
	}

	/// <inheritdoc/>
	public void Write(IEvaluationWriteTile tile)
	{
		var writeTile = tile as WriteTile;
		T[] buffer = writeTile?.MoveBuffer();

		if (buffer == null) throw new ArgumentException($"Invalid {nameof(IEvaluationTile)} to write.");
		Interlocked.Exchange(ref tileBuffers[GetTileIndex(writeTile.tilePosition)], buffer);
	}

	public void GetTileBounds(Int2 tilePosition, out Int2 min, out Int2 max)
	{
		AssertValidPosition(tilePosition, tileRange);

		min = tilePosition * tileSize;
		max = size.Min(min + tileSize);

		AssertMinMax(min, max);
	}

	Int2 GetTilePosition(Int2 position)
	{
		AssertValidPosition(position);

		return new Int2
		(
			position.X >> logTileSize.X,
			position.Y >> logTileSize.Y
		);
	}

	int GetTileIndex(Int2 tilePosition)
	{
		AssertValidPosition(tilePosition, tileRange);
		return tilePosition.Y * tileRange.X + tilePosition.X;
	}

	static int GetLocalOffset(Int2 position, Int2 min, Int2 max)
	{
		AssertMinMax(min, max);
		Int2 size = max - min;
		position -= min;

		AssertValidPosition(position, size);
		return position.Y * size.X + position.X;
	}

	[Conditional(Assert.DebugSymbol)]
	static void AssertMinMax(Int2 min, Int2 max)
	{
		Assert.IsTrue(max > min);
		Assert.IsTrue(min >= Int2.Zero);
	}

	class Tile : IEvaluationTile
	{
		public Tile(Int2 tilePosition, Int2 min, Int2 max)
		{
			AssertMinMax(min, max);
			this.tilePosition = tilePosition;

			Min = min;
			Max = max;
		}

		public readonly Int2 tilePosition;

		/// <inheritdoc/>
		public Int2 Min { get; }

		/// <inheritdoc/>
		public Int2 Max { get; }
	}

	sealed class WriteTile : Tile, IEvaluationWriteTile
	{
		public WriteTile(Int2 tilePosition, Int2 min, Int2 max) : base(tilePosition, min, max) => buffer = new T[(max - min).Product];

		T[] buffer;

		public T[] MoveBuffer() => Interlocked.Exchange(ref buffer, null);

		/// <inheritdoc/>
		Float4 IEvaluationWriteTile.this[Int2 position]
		{
			set
			{
				Assert.IsNotNull(buffer);
				int offset = GetLocalOffset(position, Min, Max);
				buffer[offset] = default(T).FromFloat4(value);
			}
		}
	}

	sealed class ReadTile : Tile, IEvaluationReadTile
	{
		public ReadTile(Int2 tilePosition, Int2 min, Int2 max, T[] buffer) : base(tilePosition, min, max)
		{
			Assert.IsNotNull(buffer);
			this.buffer = buffer;
		}

		readonly T[] buffer;

		/// <inheritdoc/>
		RGBA128 IEvaluationReadTile.this[Int2 position] => buffer[GetLocalOffset(position, Min, Max)].ToRGBA128();
	}
}