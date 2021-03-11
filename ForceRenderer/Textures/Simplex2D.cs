using System;
using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;

namespace ForceRenderer.Textures
{
	public class Simplex2D : Generative2D
	{
		public Simplex2D(Int2 size, int seed) : base(size)
		{
			Random random = new Random(seed);
			gradients = new Float2[GradientsCount];

			for (int i = 0; i < GradientsCount; i++)
			{
				float angle = (float)random.NextDouble() * 360f;
				gradients[i] = Float2.right.Rotate(angle);
			}
		}

		readonly Float2[] gradients;

		const int GradientsCount = 1024;
		const float SimplexScale = 2916f * Scalars.Sqrt2 / 125f;

		const float SquareToTriangle = (3f - Scalars.Sqrt3) / 6f;
		const float TriangleToSquare = (Scalars.Sqrt3 - 1f) / 2f;

		protected override Vector128<float> Sample(Float2 position)
		{
			Float2 skewed = position + (Float2)(position.Sum * TriangleToSquare);

			Int2 cell = skewed.Floored;
			Float2 difference = skewed - cell;

			float value = SamplePoint(position, cell) + SamplePoint(position, cell + Int2.one);

			if (difference.x >= difference.y) value += SamplePoint(position, cell + Int2.right);
			else value += SamplePoint(position, cell + Int2.up);

			value *= SimplexScale;
			value = value / 2f + 0.5f;

			return ToVector(new Float4(value, value, value, 1f));
		}

		float SamplePoint(Float2 point, Int2 cell)
		{
			float unskew = cell.Sum * SquareToTriangle;
			Float2 part = point - cell + (Float2)unskew;
			float f = 0.5f - part.SquaredMagnitude;

			if (f <= 0f) return 0f;

			f *= f * f;

			return f * gradients[cell.GetHashCode() & (GradientsCount - 1)].Dot(part);
		}
	}
}