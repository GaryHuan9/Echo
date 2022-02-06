using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Mathematics.Primitives;
/*
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

public readonly struct BoundingSphere
{
	public BoundingSphere(ReadOnlySpan<Float3> points)
	{
		Assert.IsTrue(points.Length > 2);

		if (points.Length < ExtremalPoints)
		{
			Span<Float3> extremes = stackalloc Float3[_normals.Length * 2];

			FillExtremes(points, extremes);

			SolveExact(extremes, extremes.Length, out center, out radius);
			return;
		}
		// else
		SolveExact(points, points.Length, out center, out radius);
	}

	public BoundingSphere(in Float3 center, float radius)
	{
		this.center = center;
		this.radius = radius;
	}

	const int ExtremalPoints = 6;

	static readonly Float3[] _normals =
	{
		Float3.right, Float3.up, Float3.forward
	};

	public readonly Float3 center;
	public readonly float  radius;

	// Finds extremes from given point and fills it into extremes span
	static void FillExtremes(ReadOnlySpan<Float3> points, Span<Float3> extremes)
	{
		int current = 0;
		
		foreach (Float3 normal in _normals)
		{
			float min = float.PositiveInfinity;
			float max = float.NegativeInfinity;
			Float3 min3 = Float3.zero;
			Float3 max3 = Float3.zero;

			foreach (Float3 point in points)
			{
				// do dot product
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
			extremes[++current] = max3;
			current++;
		}
	}

	static void SolveExact(ReadOnlySpan<Float3> points, int end, out Float3 center, out float radius, in int? pin1 = null, in int? pin2 = null)
	{
		int current = 0;

		// Assume the given are correct points
		if (pin1.HasValue)
		{
			if (pin2.HasValue) // pin1 && pin 2
				SolveFromExtremePoints(points[pin1.Value], points[pin2.Value], out center, out radius);
			else // pin1 only
				SolveFromExtremePoints(points[++current], points[pin1.Value], out center, out radius);
		}
		else // none
		{
			SolveFromExtremePoints(points[current], points[++current], out center, out radius);
			current++;
		}

		// Double Check, and solve the circle again if out of bounds
		for (; current < end; ++current)
		{
			if (InBound(points[current], in center, radius)) continue;
			// else not in bounds

			if (pin1.HasValue)
			{
				if (pin2.HasValue) // pin1 && pin2
					SolveFromTriangle(points[pin1.Value], points[pin2.Value], points[current], out center, out radius);
				else // pin1 only
					SolveExact(points, current, out center, out radius, pin1, current);
			}
			else // none
			{
				SolveExact(points, current, out center, out radius, current, current);
			}
		}
	}

	static bool InBound(in Float3 point, in Float3 center, float radius) => point.SquaredDistance(center) - radius <= 0f;

	static void SolveFromTriangle(in Float3 a, in Float3 b, in Float3 c, out Float3 center, out float radius)
	{
		Float3 pba = b - a;
		Float3 pca = c - a;
		Float3 planeNormal = pba.Cross(pca);

		center = (pba.SquaredMagnitude * pca - pca.SquaredMagnitude * pba).Cross(planeNormal) * .5f / planeNormal.SquaredMagnitude + a;
		radius = center.Distance(a);
	}

	static void SolveFromExtremePoints(in Float3 a, in Float3 b, out Float3 center, out float radius)
	{
		center = (a + b) * .5f;
		radius = (a - b).Magnitude * .5f;
	}
}
