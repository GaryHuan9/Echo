using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Textures.Evaluation;

/// <summary>
/// A tile-based evaluation destination; usually retrieved from and stored in a <see cref="RenderBuffer"/>.
/// Allows read and write access to the content of this layer through <see cref="IEvaluationTile"/>s.
/// </summary>
public interface IEvaluationLayer
{
	/// <summary>
	/// Creates a new <see cref="IEvaluationWriteTile"/> to write on.
	/// </summary>
	/// <param name="tilePosition">The tile position of this new tile.</param>
	/// <returns>The new <see cref="IEvaluationWriteTile"/> that was created.</returns>
	public IEvaluationWriteTile CreateTile(Int2 tilePosition);

	/// <summary>
	/// Requests a <see cref="IEvaluationReadTile"/> to read from.
	/// </summary>
	/// <param name="tilePosition">The tile position that we are requesting to read from.</param>
	/// <returns>The requested <see cref="IEvaluationReadTile"/> if it is available, otherwise null.</returns>
	public IEvaluationReadTile RequestTile(Int2 tilePosition);

	/// <summary>
	/// Applies the content of an <see cref="IEvaluationWriteTile"/> to this <see cref="IEvaluationLayer"/>.
	/// </summary>
	/// <param name="tile">The <see cref="IEvaluationWriteTile"/> to apply.</param>
	public void Apply(IEvaluationWriteTile tile);

	/// <summary>
	/// Gets the tile bounds at a particular tile position.
	/// </summary>
	/// <param name="tilePosition">The tile position to get the bounds of.</param>
	/// <param name="min">The minimum pixel position of the tile (inclusive).</param>
	/// <param name="max">The maximum pixel position of the tile (exclusive).</param>
	public void GetTileBounds(Int2 tilePosition, out Int2 min, out Int2 max);

	/// <summary>
	/// Gets the position of the tile that contains a particular pixel position.
	/// </summary>
	/// <param name="position">The input pixel position.</param>
	/// <returns>The position of the tile.</returns>
	public Int2 GetTilePosition(Int2 position);
}

/// <summary>
/// A <see cref="TextureGrid{T}"/> that is used as an evaluation destination.
/// </summary>
public class EvaluationLayer<T> : TextureGrid<T>, IEvaluationLayer where T : unmanaged, IColor<T>
{
	public EvaluationLayer(Int2 size, Int2 tileSize) : base(size)
	{
		if (!BitOperations.IsPow2(tileSize.X) || !BitOperations.IsPow2(tileSize.Y)) throw new ArgumentOutOfRangeException(nameof(tileSize));

		this.tileSize = tileSize;

		tileRange = size.CeiledDivide(tileSize);
		tiles = new ReadTile[tileRange.Product];

		logTileSize = new Int2
		(
			BitOperations.Log2((uint)tileSize.X),
			BitOperations.Log2((uint)tileSize.Y)
		);
	}

	/// <summary>
	/// The maximum size of the individual tiles. Some edge tiles might be smaller, since they could be cropped.
	/// </summary>
	public readonly Int2 tileSize;

	/// <summary>
	/// The number of tiles on the X and Y axis. This equals <see cref="TextureGrid{T}.size"/> divided by <see cref="tileSize"/> (rounding up).
	/// </summary>
	public readonly Int2 tileRange;

	readonly ReadTile[] tiles;
	readonly Int2 logTileSize;

	public override T this[Int2 position]
	{
		get
		{
			Int2 tilePosition = GetTilePosition(position);
			int tileIndex = GetTileIndex(tilePosition);
			return tiles[tileIndex]?[position] ?? default;
		}
	}

	/// <inheritdoc/>
	public IEvaluationWriteTile CreateTile(Int2 tilePosition)
	{
		EnsureValidPosition(tilePosition, tileRange);
		GetTileBounds(tilePosition, out Int2 min, out Int2 max);
		return new WriteTile(tilePosition, min, max);
	}

	/// <inheritdoc/>
	public IEvaluationReadTile RequestTile(Int2 tilePosition)
	{
		EnsureValidPosition(tilePosition, tileRange);
		return tiles[GetTileIndex(tilePosition)];
	}

	/// <inheritdoc/>
	public void Apply(IEvaluationWriteTile tile)
	{
		var writeTile = tile as WriteTile;
		T[] buffer = writeTile?.MoveBuffer();

		if (buffer == null) throw new ArgumentException($"Invalid {nameof(IEvaluationTile)} to write.", nameof(tile));

		GetTileBounds(writeTile.tilePosition, out Int2 min, out Int2 max);
		var readTile = new ReadTile(writeTile.tilePosition, min, max, buffer);
		ref ReadTile destination = ref tiles[GetTileIndex(writeTile.tilePosition)];
		Interlocked.Exchange(ref destination, readTile);
	}

	/// <summary>
	/// Fully clears the content of this <see cref="EvaluationLayer{T}"/>.
	/// </summary>
	public void Clear()
	{
		foreach (ref ReadTile tile in tiles.AsSpan()) Interlocked.Exchange(ref tile, null);
	}

	/// <inheritdoc/>
	public void GetTileBounds(Int2 tilePosition, out Int2 min, out Int2 max)
	{
		EnsureValidPosition(tilePosition, tileRange);

		min = tilePosition * tileSize;
		max = size.Min(min + tileSize);

		EnsureMinMax(min, max);
	}

	/// <inheritdoc/>
	public Int2 GetTilePosition(Int2 position)
	{
		EnsureValidPosition(position);

		return new Int2
		(
			position.X >> logTileSize.X,
			position.Y >> logTileSize.Y
		);
	}

	int GetTileIndex(Int2 tilePosition)
	{
		EnsureValidPosition(tilePosition, tileRange);
		return tilePosition.Y * tileRange.X + tilePosition.X;
	}

	static int GetLocalOffset(Int2 position, Int2 min, Int2 max)
	{
		EnsureMinMax(min, max);
		Int2 size = max - min;
		position -= min;

		EnsureValidPosition(position, size);
		return position.Y * size.X + position.X;
	}

	[Conditional("DEBUG")]
	static void EnsureMinMax(Int2 min, Int2 max)
	{
		Ensure.IsTrue(max > min);
		Ensure.IsTrue(min >= Int2.Zero);
	}

	class Tile : IEvaluationTile
	{
		public Tile(Int2 tilePosition, Int2 min, Int2 max)
		{
			EnsureMinMax(min, max);
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
				Ensure.IsNotNull(buffer);
				int offset = GetLocalOffset(position, Min, Max);
				buffer[offset] = default(T).FromFloat4(value);
			}
		}
	}

	sealed class ReadTile : Tile, IEvaluationReadTile
	{
		public ReadTile(Int2 tilePosition, Int2 min, Int2 max, T[] buffer) : base(tilePosition, min, max)
		{
			Ensure.IsNotNull(buffer);
			this.buffer = buffer;
		}

		readonly T[] buffer;

		public T this[Int2 position] => buffer[GetLocalOffset(position, Min, Max)];

		/// <inheritdoc/>
		RGBA128 IEvaluationReadTile.this[Int2 position] => this[position].ToRGBA128();
	}
}