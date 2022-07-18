using System;
using Echo.Core.Common.Packed;

namespace Echo.Terminal.Core.Display;

public record struct Brush(Int2 Position, TextOptions options)
{
	public Brush() : this(new TextOptions()) { }

	public Brush(TextOptions options) : this(0, options) { }

	public Brush(int y) : this(y, new TextOptions()) { }

	public Brush(int y, TextOptions options) : this(new Int2(0, y), options) { }

	/// <summary>
	/// Options used to control the output of texts.
	/// </summary>
	public readonly TextOptions options = options;

	/// <summary>
	/// The current X position of the <see cref="Brush"/>.
	/// </summary>
	public int X
	{
		readonly get => Position.X;
		set => Position = new Int2(value, Y);
	}

	/// <summary>
	/// The current Y position of the <see cref="Brush"/>.
	/// </summary>
	public int Y
	{
		readonly get => Position.Y;
		set => Position = new Int2(X, value);
	}

	/// <summary>
	/// The current position of the <see cref="Brush"/>.
	/// </summary>
	public Int2 Position { get; set; } = Position;

	/// <summary>
	/// Moves this <see cref="Brush"/> to the next line.
	/// </summary>
	public void NextLine() => Position = new Int2(0, Y + 1);

	/// <summary>
	/// Moves this <see cref="Brush"/> to the start of this current line.
	/// </summary>
	public void CarriageReturn() => Position = new Int2(0, Y);

	/// <summary>
	/// Checks if there is more room for this <see cref="Brush"/> to write.
	/// </summary>
	/// <param name="size">The size to be checked against.</param>
	/// <returns>True if there is no space remaining to write, false otherwise.</returns>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="Position"/> is invalid.</exception>
	public readonly bool CheckBounds(Int2 size)
	{
		if ((Int2.Zero <= Position) & (Position < size)) return false;
		if ((Position.X == 0) & (Position.Y == size.Y)) return true;
		throw new InvalidOperationException($"{nameof(Position)} out of bounds.");
	}
}