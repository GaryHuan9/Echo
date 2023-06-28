using System;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;

namespace Echo.Core.Textures.Colors;

/// <summary>
/// A 128-bit implementation of <see cref="IColor{T}"/> that stores normal and depth information together.
/// </summary>
public readonly struct NormalDepth128 : IColor<NormalDepth128>, IFormattable
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
		float alpha = 1f / (depth + 1f);
		Float4 color = ((Float4)normal + Float4.One) * 0.5f;
		return new RGBA128(color.X, color.Y, color.Z, alpha);
	}

	/// <inheritdoc/>
	public NormalDepth128 FromRGBA128(RGBA128 value) => new
	(
		(Float3)(value - Float4.Half).XYZ_.Normalized,
		1f / FastMath.Max(value.Alpha, FastMath.Epsilon) - 1f
	);

	/// <inheritdoc/>
	public Float4 ToFloat4() => new(normal.X, normal.Y, normal.Z, depth);

	/// <inheritdoc/>
	public NormalDepth128 FromFloat4(Float4 value) => new(new Float3(value.X, value.Y, value.Z).Normalized, value.W);

	public override string ToString() => ToString(default);

	/// <inheritdoc/>
	public string ToString(string format, IFormatProvider provider = null) => $"n: {normal} d: {depth}";
}