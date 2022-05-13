namespace Echo.Terminal.Core.Display;

/// <summary>
/// Options used to format texts.
/// </summary>
public readonly record struct TextOptions
{
	public TextOptions() { }

	/// <summary>
	/// Defines the <see cref="Display.WrapOptions"/> for texts.
	/// </summary>
	public WrapOptions WrapOptions { get; init; } = WrapOptions.WordBreak;

	/// <summary>
	/// If true, ellipsis `…` will be used to signify texts that overflew.
	/// Otherwise, texts will be simply truncated without any indication.
	/// </summary>
	public bool Ellipsis { get; init; } = true;
}

/// <summary>
/// Options used to layout texts.
/// </summary>
public enum WrapOptions : byte
{
	/// <summary>
	/// Gaps between individual words are spaced out to occupy all available space from wrapping.
	/// </summary>
	Justified = default,

	/// <summary>
	/// Individual words wrap around horizontal border and extend vertically with jagged edges.
	/// </summary>
	WordBreak,

	/// <summary>
	/// Texts wrap around horizontal border without restrictions and extend vertically.
	/// </summary>
	LineBreak,

	/// <summary>
	/// Texts only occupy one vertical line and overflow horizontally.
	/// </summary>
	NoWrap
}