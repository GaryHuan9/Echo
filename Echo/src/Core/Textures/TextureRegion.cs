using System;
using CodeHelpers.Packed;
using Echo.Common.Mathematics;

namespace Echo.Core.Textures;

/// <summary>
/// A rectangular area in a <see cref="Texture"/>, defined by a <see cref="size"/> and an <see cref="offset"/>.
/// </summary>
public readonly struct TextureRegion
{
	public TextureRegion(Float2 size, Float2 offset)
	{
		this.size = size;
		this.offset = offset;
	}

	public readonly Float2 size;
	public readonly Float2 offset;

	/// <summary>
	/// Converts a normalized coordinate into a texture coordinate based on this <see cref="TextureRegion"/>.
	/// </summary>
	/// <param name="uv">The normalized coordinate that is between zero and one.</param>
	/// <returns>The texture coordinate as defined by this <see cref="TextureRegion"/>.</returns>
	public Float2 GetTexcoord(Float2 uv) => new
	(
		FastMath.FMA(uv.X, size.X, offset.X),
		FastMath.FMA(uv.Y, size.Y, offset.Y)
	);
}