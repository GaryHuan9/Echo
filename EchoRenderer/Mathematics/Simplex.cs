using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Mathematics
{
	public class Simplex
	{
		public Simplex(int seed)
		{
			Random random = new Random(seed);
			directions = new Float2[DirectionsCount];

			for (int i = 0; i < DirectionsCount; i++)
			{
				float angle = (float)random.NextDouble() * 360f;
				directions[i] = Float2.right.Rotate(angle);
			}
		}

		readonly Float2[] directions;

		const int DirectionsCount = 1024;
		const float SimplexScale = 2916f * Scalars.Sqrt2 / 125f;

		const float SquareToTriangle = (3f - Scalars.Sqrt3) / 6f;
		const float TriangleToSquare = (Scalars.Sqrt3 - 1f) / 2f;

		public float Sample(Float2 position)
		{
			Float2 skewed = position + (Float2)(position.Sum * TriangleToSquare);

			Int2 cell = skewed.Floored;
			Float2 part = skewed - cell;

			float value = SamplePoint(position, cell) + SamplePoint(position, cell + Int2.one);
			value += SamplePoint(position, cell + (part.x >= part.y ? Int2.right : Int2.up));

			return value * SimplexScale / 2f + 0.5f;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		float SamplePoint(Float2 point, Int2 cell)
		{
			Float2 part = point - cell + (Float2)(cell.Sum * SquareToTriangle);
			float weight = 0.5f - part.SquaredMagnitude;

			if (weight <= 0f) return 0f;
			weight *= weight * weight;

			return weight * directions[cell.GetHashCode() & (DirectionsCount - 1)].Dot(part);
		}
	}
}