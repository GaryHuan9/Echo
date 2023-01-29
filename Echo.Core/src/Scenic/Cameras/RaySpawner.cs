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
	/// <param name="layer">The destination <see cref="IEvaluationLayer"/>; this object must also be a <see cref="TextureGrid"/>.</param>
	/// <param name="position">The location on the <see cref="IEvaluationLayer"/> this <see cref="RaySpawner"/> originates.</param>
	public RaySpawner(IEvaluationLayer layer, Int2 position)
	{
		var texture = (TextureGrid)layer;
		
		sizeR = texture.sizeR;
		offsets = texture.aspects / -2f;
		this.position = position;
	}

	public readonly Int2 position;
	readonly Float2 sizeR;
	readonly Float2 offsets;

	/// <summary>
	/// Converts a normalized coordinate into a coordinate normalized on the X axis.
	/// </summary>
	/// <param name="uv">The normalized coordinate that is between zero and one.</param>
	/// <returns>The coordinate that is normalized on the X axis based on this <see cref="RaySpawner"/>.</returns>
	/// <remarks>The returned <see cref="Float2.X"/> component is between -0.5f and 0.5f,
	/// and the <see cref="Float2.Y"/> component is scaled proportionally around zero.</remarks>
	public Float2 SpawnX(Float2 uv)
	{
		uv += position;

		return new Float2
		(
			FastMath.FMA(uv.X, sizeR.X, 1f / -2f),
			FastMath.FMA(uv.Y, sizeR.X, offsets.Y)
		);
	}

	/// <summary>
	/// Converts a normalized coordinate into a coordinate normalized on the Y axis.
	/// </summary>
	/// <param name="uv">The normalized coordinate that is between zero and one.</param>
	/// <returns>The coordinate that is normalized on the Y axis based on this <see cref="RaySpawner"/>.</returns>
	/// <remarks>The returned <see cref="Float2.Y"/> component is between -0.5f and 0.5f,
	/// and the <see cref="Float2.X"/> component is scaled proportionally around zero.</remarks>
	public Float2 SpawnY(Float2 uv)
	{
		uv += position;

		return new Float2
		(
			FastMath.FMA(uv.X, sizeR.Y, offsets.X),
			FastMath.FMA(uv.Y, sizeR.Y, 1f / -2f)
		);
	}
}