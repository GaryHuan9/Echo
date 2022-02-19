using System;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Common.Mathematics.Primitives;

public readonly struct BoundingSphere
{
	public BoundingSphere(ReadOnlySpan<Float3> points)
	{
		SolveExact(points, out center, out radius);

		/*if (points.Length < ExtremalPoints)
		{
			Span<Float3> extremes = stackalloc Float3[normals.Length * 2];

			FillExtremes(points, extremes);

			SolveExactOld(extremes, out center, out radius);
			return;
		}
		// else there is no reason for us to solve less

		SolveExactOld(points, out center, out radius);*/
	}

	public BoundingSphere(in Float3 center, float radius)
	{
		this.center = center;
		this.radius = radius;
	}


	const int NormalCount = 3;
	const int ExtremalPoints = NormalCount * 2;

	// NOTE: The count of normals could be increased if required for precision, for now we'll be using 3
	static readonly Float3[] normals =
	{
		Float3.right, Float3.up, Float3.forward
	};

	public readonly Float3 center;
	public readonly float radius;

	/// <summary>
	/// Returns whether <paramref name="point"/> is inside this <see cref="BoundingSphere"/>.
	/// </summary>
	public bool Contains(in Float3 point) => Contains(point, in center, radius);

	// Finds extremes from given point using the normals and fills it into extremes span
	static void FillExtremes(ReadOnlySpan<Float3> points, Span<Float3> extremes)
	{
		int current = 0;

		foreach (Float3 normal in normals)
		{
			float min = float.PositiveInfinity;
			float max = float.NegativeInfinity;
			Float3 min3 = Float3.zero;
			Float3 max3 = Float3.zero;

			foreach (ref readonly Float3 point in points)
			{
				// dot product with normal to find the extreme points efficiently
				float value = point.Dot(normal);

				if (value < min)
				{
					min = value;
					min3 = point;
				}

				if (value > max)
				{
					max = value;
					max3 = point;
				}
			}

			extremes[current] = min3;
			extremes[current + 1] = max3;
			current += 2;
		}
	}

	/// <summary>
	///     Solves exact sphere using the mini-ball implementation from paper
	/// </summary>
	static void SolveExact(ReadOnlySpan<Float3> points, out Float3 center, out float radius)
	{
		//set center as first point
		center = points[0];

		//find farthest
		int farthest = 0; // not in use

		radius = 0;

		float sqrRadius = 0;

		for (int i = 1; i < points.Length; ++i)
		{
			Float3 point = points[i];

			float distance = center.SquaredDistance(point);

			if (distance < sqrRadius) continue;
			//else

			sqrRadius = distance;
			farthest = i;

			radius = MathF.Sqrt(sqrRadius);
		}
	}

	/// <summary>
	///     Solves the exact sphere using the given points, reference: https://www.youtube.com/watch?v=HojzdCICjmQ&t=260s
	/// </summary>
	static void SolveExactOld(ReadOnlySpan<Float3> points, out Float3 center, out float radius)
	{
		SolveFromExtremePoints(points[0], points[1], out center, out radius);

		// Double Check, and solve the circle again if out of bounds
		for (int current = 2; current < points.Length; ++current)
		{
			if (Contains(points[current], in center, radius)) continue;
			// else not in bounds

			SolveExactWithPins(points, current, out center, out radius, current, current);
		}
	}

	/// <summary>
	///     Solves the exact sphere using the given points with anchored pins (or you could say guessed extreme points)
	/// </summary>
	static void SolveExactWithPins(ReadOnlySpan<Float3> points, int end, out Float3 center, out float radius, int pin1, int pin2)
	{
		SolveFromExtremePoints(points[pin1], points[pin2], out center, out radius);

		for (int current = 0; current < end; ++current)
		{
			if (Contains(points[current], in center, radius)) continue;
			// else not in bounds

			SolveFromTriangle(points[pin1], points[pin2], points[current], out center, out radius);
		}
	}

	static bool Contains(in Float3 point, in Float3 center, float radius) => center.SquaredDistance(point) <= radius * radius;

	/// <summary>
	///     Solves the CircumSphere from three points
	/// </summary>
	static void SolveFromTriangle(in Float3 a, in Float3 b, in Float3 c, out Float3 center, out float radius)
	{
		Float3 pba = b - a;
		Float3 pca = c - a;
		Float3 planeNormal = pba.Cross(pca);

		center = (pba.SquaredMagnitude * pca - pca.SquaredMagnitude * pba).Cross(planeNormal) / 2f / planeNormal.SquaredMagnitude + a;
		radius = center.Distance(a);
	}

	/// <summary>
	///     Solves the sphere from two extreme points (assuming they are the sphere's diameter end points)
	/// </summary>
	static void SolveFromExtremePoints(in Float3 a, in Float3 b, out Float3 center, out float radius)
	{
		center = (a + b) / 2f;
		radius = (a - b).Magnitude / 2f;
	}
}

/*
 * Definitions:
 * n = num of points; k = number of extremal points [Constant], we use 6 for now; s = number of normals = k / 2 = len(N);
 * P = Points set; N = Normals set;
 * E = Extremal points set; S' = minimum sphere, S = result sphere
 * ==========================================================================================
 * Pseudo Code:
 * if (n > (k <- 2s)) then
 *    E <- FindExtremalPoints(P, N)
 *    S' <- MinimumSphere(E)
 *    S <- GrowSphere(P, S')
 * else
 *    S <- MinimumSphere(P)
 * ==========================================================================================
 * Explanation:
 * check if the given points are in required amount of points for quick processing
 *  True:
 *      Finds all the extremal points using The given normals with dot product
 *      Use the exact solver to solve out the new minimum sphere
 *      Now use "GrowSphere" to loop check through all points
 *      Each time a point outside the current sphere is encountered, a new larger sphere
 *      enclosing the current sphere and the point is computed (or you could say use the points outside the sphere as )
 */