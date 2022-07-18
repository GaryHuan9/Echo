using System;
using System.Runtime.CompilerServices;
using Echo.Core.Common.Packed;

namespace Echo.Core.Common.Mathematics.Randomization;

/// <summary>
/// Standard simplex gradient noise generator
/// </summary>
public class Simplex
{
	/// <summary>
	/// Creates a standard simplex gradient noise generator
	/// </summary>
	/// <param name="seed">Generators with the same seed will generate the same value at the same location.</param>
	/// <param name="directionCount">The amount of directions used for generation. Does not majorly affect quality.</param>
	public Simplex(int seed, int directionCount = 256)
	{
		Random random = new Random(seed);

		this.directionCount = directionCount;
		directions = new Float2[directionCount];

		for (int i = 0; i < directionCount; i++)
		{
			float angle = (float)random.NextDouble() * 360f;
			directions[i] = Float2.Right.Rotate(angle);
		}
	}

	readonly Float2[] directions;
	readonly int directionCount;

	const float SimplexScale = 2916f * Scalars.Root2 / 125f;

	const float SquareToTriangle = (3f - Scalars.Root3) / 6f;
	const float TriangleToSquare = (Scalars.Root3 - 1f) / 2f;

	/// <summary>
	/// Retrieves the value of the noise at <paramref name="position"/>.
	/// Returned values are always between the range of 0 and 1.
	/// </summary>
	public float Sample(Float2 position)
	{
		Float2 skewed = position + (Float2)(position.Sum * TriangleToSquare);

		Int2 cell = skewed.Floored;
		Float2 part = skewed - cell;

		float value = SamplePoint(position, cell) + SamplePoint(position, cell + Int2.One);
		value += SamplePoint(position, cell + (part.X >= part.Y ? Int2.Right : Int2.Up));

		return value * SimplexScale / 2f + 0.5f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float SamplePoint(Float2 point, Int2 cell)
	{
		Float2 part = point - cell + (Float2)(cell.Sum * SquareToTriangle);
		float weight = 0.5f - part.SquaredMagnitude;

		if (weight <= 0f) return 0f;
		weight *= weight * weight;

		return weight * directions[cell.GetHashCode() & (directionCount - 1)].Dot(part);
	}
}