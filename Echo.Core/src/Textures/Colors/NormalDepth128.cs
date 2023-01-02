using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;

namespace Echo.Core.Textures.Colors;

public readonly struct NormalDepth128 : IColor<NormalDepth128>
{
	public NormalDepth128(Float3 normal, float depth)
	{
		Ensure.AreEqual(normal.SquaredMagnitude, 1f);
		Ensure.IsTrue(depth >= 0f && !float.IsPositiveInfinity(depth));

		this.normal = normal;
		this.depth = depth;
	}

	public readonly Float3 normal;
	public readonly float depth;

	/// <inheritdoc/>
	public RGBA128 ToRGBA128()
	{
		Float3 c = (normal + Float3.One) * 0.5f;
		return new RGBA128(c.X, c.Y, c.Z, depth);
	}

	/// <inheritdoc/>
	public NormalDepth128 FromRGBA128(in RGBA128 value)
	{
		Float4 n = (value - Float4.Half).XYZ_.Normalized;
		return new NormalDepth128((Float3)n, value.Alpha);
	}

	/// <inheritdoc/>
	public Float4 ToFloat4() => new(normal.X, normal.Y, normal.Z, depth);

	/// <inheritdoc/>
	public NormalDepth128 FromFloat4(in Float4 value) => new(new Float3(value.X, value.Y, value.Z).Normalized, depth);
}