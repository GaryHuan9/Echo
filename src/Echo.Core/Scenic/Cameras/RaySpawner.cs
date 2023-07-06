using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Evaluation;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Scenic.Cameras;

/// <summary>
/// A container that stores information necessary to spawning a <see cref="Ray"/> from a <see cref="RenderTexture"/>.
/// </summary>
public readonly struct RaySpawner
{
	/// <summary>
	/// Constructs a new <see cref="RaySpawner"/>.
	/// </summary>
	/// <param name="texture">The destination <see cref="TextureGrid"/>; most often this is also an <see cref="IEvaluationLayer"/>.</param>
	/// <param name="position">A pixel location on the <see cref="TextureGrid"/> this <see cref="RaySpawner"/> originates from.</param>
	public RaySpawner(TextureGrid texture, Int2 position)
	{
		sizeR = texture.sizeR;
		offsets = texture.aspects / -2f;
		this.position = position;
	}

	public readonly Int2 position;
	public readonly Float2 sizeR;
	readonly Float2 offsets;

	/// <summary>
	/// Converts a shift within the pixel into a normalized coordinate on the texture.
	/// </summary>
	/// <param name="shift">A normalized shift between zero and one within the spawning pixel.</param>
	/// <returns>The texture coordinate normalized on the X axis based on this <see cref="RaySpawner"/>.</returns>
	/// <remarks>The returned <see cref="Float2.X"/> component is between -0.5f and 0.5f,
	/// and the <see cref="Float2.Y"/> component is scaled proportionally around zero.</remarks>
	public Float2 SpawnX(Float2 shift)
	{
		shift += position;

		return new Float2
		(
			FastMath.FMA(shift.X, sizeR.X, 1f / -2f),
			FastMath.FMA(shift.Y, sizeR.X, offsets.Y)
		);
	}

	/// <summary>
	/// Converts a shift within the pixel into a normalized coordinate on the texture.
	/// </summary>
	/// <param name="shift">A normalized shift between zero and one within the spawning pixel.</param>
	/// <returns>The texture coordinate normalized on the Y axis based on this <see cref="RaySpawner"/>.</returns>
	/// <remarks>The returned <see cref="Float2.Y"/> component is between -0.5f and 0.5f,
	/// and the <see cref="Float2.X"/> component is scaled proportionally around zero.</remarks>
	public Float2 SpawnY(Float2 shift)
	{
		shift += position;

		return new Float2
		(
			FastMath.FMA(shift.X, sizeR.Y, offsets.X),
			FastMath.FMA(shift.Y, sizeR.Y, 1f / -2f)
		);
	}
}