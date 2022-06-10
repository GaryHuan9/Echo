using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grid;

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
	IEvaluationWriteTile CreateTile(Int2 tilePosition);

	/// <summary>
	/// Requests a <see cref="IEvaluationReadTile"/> to read from.
	/// </summary>
	/// <param name="tilePosition">The tile position that we are requesting to read from.</param>
	/// <returns>The requested <see cref="IEvaluationReadTile"/> if it is available, otherwise null.</returns>
	IEvaluationReadTile RequestTile(Int2 tilePosition);

	/// <summary>
	/// Applies the content of an <see cref="IEvaluationWriteTile"/> to this <see cref="IEvaluationLayer"/>.
	/// </summary>
	/// <param name="tile">The <see cref="IEvaluationWriteTile"/> to apply.</param>
	void Apply(IEvaluationWriteTile tile);
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
		tileBuffers = new T[tileRange.Product][];

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

	readonly T[][] tileBuffers;
	readonly Int2 logTileSize;

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
	public void Apply(IEvaluationWriteTile tile)
	{
		var writeTile = tile as WriteTile;
		T[] buffer = writeTile?.MoveBuffer();

		if (buffer == null) throw new ArgumentException($"Invalid {nameof(IEvaluationTile)} to write.");
		Interlocked.Exchange(ref tileBuffers[GetTileIndex(writeTile.tilePosition)], buffer);
	}

	/// <summary>
	/// Fully clears the content of this <see cref="EvaluationLayer{T}"/>.
	/// </summary>
	public void Clear()
	{
		foreach (ref T[] buffer in tileBuffers.AsSpan()) Interlocked.Exchange(ref buffer, null);
	}

	/// <summary>
	/// Gets the tile bounds at a particular tile position.
	/// </summary>
	/// <param name="tilePosition">The tile position to get the bounds of.</param>
	/// <param name="min">The minimum pixel position of the tile (inclusive).</param>
	/// <param name="max">The maximum pixel position of the tile (exclusive).</param>
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