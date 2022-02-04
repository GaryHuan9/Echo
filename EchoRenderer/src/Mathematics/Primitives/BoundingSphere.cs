using System;
using System.Runtime.InteropServices;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Mathematics.Primitives
{
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

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public readonly struct BoundingSphere
    {
        public BoundingSphere(ReadOnlySpan<Float3> points)
        {
            if(points.Length < EXTREMAL_POINTS)
            {
                var extremes = new ReadOnlySpan<Float3>(FindExtremalPoints(in points));
                SolveExact(extremes, out center, out radius);
                return;
            }
            // else
            SolveExact(points, out center, out radius);
            return;
            
            // functions

            Float3[] FindExtremalPoints(in ReadOnlySpan<Float3> points)
            {
                int current = 0;

                var extremes = new Float3[normals.Length * 2];

                foreach(Float3 normal in normals)
                {
                    float min = float.PositiveInfinity;
                    float max = float.NegativeInfinity;
                    Float3 min3 = Float3.zero;
                    Float3 max3 = Float3.zero;

                    foreach(Float3 point in points)
                    {
                        // do dot product
                        float value = point.Dot(normal);
                        if(value < min)
                        {
                            min = value;
                            min3 = point;
                        }
                        if(value > max)
                        {
                            max = value;
                            max3 = point;
                        }
                    }

                    extremes[current] = min3;
                    extremes[++current] = max3;
                    current++;
                }

                return extremes;
            }

            void SolveExact(ReadOnlySpan<Float3> points, out Float3 center, out float radius)
            {
                /*int current = 0;
                
                SolveFromDiameterPoints(points[current], points[current + 1], out center, out radius);
                
                // check all
                foreach(Float3 point in points)
                {
                    if(InBound(point, in center, in radius)) continue;
                    // else not in bound
                    
                }*/

                throw new NotImplementedException("needed to learn Gartner's algorithm also to do this");
            }

            bool InBound(in Float3 point, in Float3 center, in float radius) => point.SquaredDistance(center) - radius <= 0f;

            void SolveFromDiameterPoints(Float3 a, Float3 b, out Float3 center, out float radius)
            {
                center = (a + b) * .5f;
                radius = (a - b).Magnitude * .5f;
            }
        }

        public BoundingSphere(in Float3 center, in float radius)
        {
            this.center = center;
            this.radius = radius;
        }

        const int EXTREMAL_POINTS = 6;

        static readonly Float3[] normals =
        {
            new(1, 0, 0), new(0, 1, 0), new(0, 0, 1)
        };

        [FieldOffset(0)]  public readonly Float3 center;
        [FieldOffset(12)] public readonly float  radius;
    }
}
