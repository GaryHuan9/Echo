using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Distributions;
using EchoRenderer.Core.Scenic.Preparation;

namespace EchoRenderer.Core.Scenic.Geometries;

public class SphereEntity : GeometryEntity
{
	public float Radius { get; set; } = 1f;

	public override IEnumerable<PreparedTriangle> ExtractTriangles(SwatchExtractor extractor) => Enumerable.Empty<PreparedTriangle>();

	public override IEnumerable<PreparedSphere> ExtractSpheres(SwatchExtractor extractor)
	{
		if (Radius <= 0f || FastMath.AlmostZero(Radius)) yield break;
		yield return new PreparedSphere(this, extractor.Register(Material));
	}
}

public readonly struct PreparedSphere
{
	public PreparedSphere(SphereEntity sphere, MaterialIndex material) : this
	(
		sphere.LocalToWorld.MultiplyPoint(Float3.zero),
		sphere.Scale.MaxComponent * sphere.Radius,
		material
	) { }

	public PreparedSphere(in Float3 position, float radius, MaterialIndex material)
	{
		this.position = position;
		this.radius = radius;
		this.material = material;
	}

	public readonly Float3 position;
	public readonly float radius;
	public readonly MaterialIndex material;

	/// <summary>
	/// The smallest <see cref="AxisAlignedBoundingBox"/> that encloses this <see cref="PreparedSphere"/>.
	/// </summary>
	public AxisAlignedBoundingBox AABB => new(position - (Float3)radius, position + (Float3)radius);

	/// <summary>
	/// The area of this <see cref="PreparedSphere"/>.
	/// </summary>
	public float Area => 2f * Scalars.TAU * radius * radius;

	/// <summary>
	/// Because spheres have two intersection points, if an intersection distance is going to be under this value,
	/// we have the option to attempt to find the intersection that is further away. This is used to avoid self
	/// intersections along with <see cref="TraceQuery.ignore"/> and <see cref="OccludeQuery.ignore"/>.
	/// </summary>
	const float DistanceThreshold = 6e-4f;

	/// <summary>
	/// Returns the distance of intersection between this <see cref="PreparedSphere"/> and <paramref name="ray"/> without
	/// backface culling. If the intersection exists, the distance is returned and <paramref name="uv"/> will contain the
	/// a coordinate representing the surface location of the intersection, otherwise, <see cref="float.PositiveInfinity"/> is returned.
	/// NOTE: if <paramref name="findFar"/> is true, any intersection distance under <see cref="DistanceThreshold"/> is ignored.
	/// </summary>
	public float Intersect(in Ray ray, out Float2 uv, bool findFar = false)
	{
		const float Infinity = float.PositiveInfinity;
		Unsafe.SkipInit(out uv);

		//Test ray direction
		Float3 offset = ray.origin - position;
		float radius2 = radius * radius;
		float center = -offset.Dot(ray.direction);

		float extend2 = FastMath.FSA(center, radius2 - offset.SquaredMagnitude);

		if (extend2 < 0f) return Infinity;

		//Find appropriate distance
		float extend = FastMath.Sqrt0(extend2);
		float distance = center - extend;

		float threshold = findFar ? DistanceThreshold : 0f;

		if (distance < threshold) distance = center + extend;
		if (distance < threshold) return Infinity;

		//Calculate uv
		Float3 point = offset + ray.direction * distance;
		float sinP = FastMath.Clamp11(point.y / radius);
		float sinT = 0f;

		float smallRadius = FastMath.FMA(-point.y, point.y, radius2);
		if (smallRadius > 0f) sinT = point.x * FastMath.SqrtR0(smallRadius);
		if (point.z < 0f) sinT += 3f; //Move sinT out of domain when cosT should be negative

		//Return
		uv = new Float2(sinT, sinP);
		return distance;
	}

	/// <summary>
	/// Returns whether <paramref name="ray"/> will intersect with this <see cref="PreparedSphere"/> after <paramref name="travel"/>.
	/// NOTE: if <paramref name="findFar"/> is true, any intersection distance under <see cref="DistanceThreshold"/> is ignored.
	/// </summary>
	public bool Intersect(in Ray ray, float travel, bool findFar = false)
	{
		//Test ray direction
		Float3 offset = ray.origin - position;
		float center = -offset.Dot(ray.direction);

		float squared = FastMath.FSA(center, FastMath.FSA(radius, -offset.SquaredMagnitude));

		if (squared < 0f) return false;

		//Find appropriate distance
		float extend = FastMath.Sqrt0(squared);
		float distance = center - extend;

		float threshold = findFar ? DistanceThreshold : 0f;

		if (distance < threshold) distance = center + extend;
		return distance >= threshold && distance < travel;
	}

	/// <summary>
	/// Samples this <see cref="PreparedSphere"/> based on <paramref name="sample"/> at <paramref name="origin"/> and
	/// outputs the probability density function <paramref name="pdf"/> over solid angles from <paramref name="origin"/>.
	/// </summary>
	public GeometryPoint Sample(in Float3 origin, Sample2D sample, out float pdf)
	{
		//Check whether origin is inside sphere
		Float3 offset = origin - position;
		float radius2 = radius * radius;
		float length2 = offset.SquaredMagnitude;

		if (length2 <= radius2)
		{
			//Sample uniformly if is inside
			var point = GetPoint(sample.UniformSphere);
			pdf = point.ProbabilityDensity(origin, Area);

			return point;
		}

		//Find cosine max
		float sinMaxT2 = radius2 / length2;
		float cosMaxT = FastMath.Sqrt0(1f - sinMaxT2);

		//Uniform sample cone, defined by theta and phi
		float cosT = FastMath.FMA(sample.x, cosMaxT - 1f, 1f);
		float sinT = FastMath.Identity(cosT);
		float phi = sample.y * Scalars.TAU;

		//Compute angle alpha from center of sphere to sample point
		float length = FastMath.Sqrt0(length2);
		float project = length * cosT - FastMath.Sqrt0(radius2 - length2 * sinT * sinT);
		float cosA = (length2 + radius2 - project * project) / (2f * length * radius);
		float sinA = FastMath.Identity(cosA);

		//Calculate normal and pdf
		FastMath.SinCos(phi, out float sinP, out float cosP);
		Float3 normal = new Float3(sinA * cosP, sinA * sinP, cosA);

		pdf = ProbabilityDensityCone(cosMaxT);

		//Transform and returns point
		var transform = new NormalTransform(offset / length);
		return GetPoint(transform.LocalToWorld(normal));
	}

	/// <summary>
	/// Returns the probability density function over solid angles of sampling <paramref name="incident"/> from <paramref name="origin"/>.
	/// </summary>
	public float ProbabilityDensity(in Float3 origin, in Float3 incident)
	{
		//Check whether point is inside this sphere
		Float3 offset = origin - position;
		float radius2 = radius * radius;
		float length2 = offset.SquaredMagnitude;

		if (length2 <= radius2)
		{
			//Find intersection with sphere when is inside
			float projected = offset.Dot(incident);
			float extend2 = FastMath.FSA(projected, radius2 - length2);

			//Find the un-normalized normal at our point of intersection
			float distance = FastMath.Sqrt0(extend2) - projected; //The distance to our point on the sphere
			Float3 normal = offset + incident * distance;         //The un-normalized normal of our point

			float cosWeight = incident.Dot(normal) * radius;
			if (FastMath.AlmostZero(cosWeight)) return 0f;

			return distance * distance / FastMath.Abs(cosWeight) * Sample2D.UniformSpherePdf;
		}

		//Since the point is not inside our sphere, the sampling is based on a cone is not uniform
		// if (!Intersect(new Ray(origin, incident), float.PositiveInfinity)) return 0f;
		//TODO: try and see if this line actually does something useful, and if it does, inline the intersection computation

		float sinMaxT2 = radius2 / length2;
		float cosMaxT = FastMath.Sqrt0(1f - sinMaxT2);
		return ProbabilityDensityCone(cosMaxT);
	}

	GeometryPoint GetPoint(in Float3 normal) => new(normal * radius + position, normal);

	public static Float3 GetNormal(Float2 uv)
	{
		ToThetaPhi(uv, out float sinT, out float sinP, out float cosT);
		float cosP = FastMath.Identity(sinP);
		return new Float3(sinT * cosP, sinP, cosT * cosP).Normalized;
	}

	public static Float2 GetTexcoord(Float2 uv)
	{
		ToThetaPhi(uv, out float sinT, out float sinP, out float cosT);

		return new Float2
		(
			FastMath.FMA(MathF.Atan2(sinT, cosT), 1f / Scalars.TAU, 0.5f),
			FastMath.FMA(MathF.Asin(FastMath.Clamp11(sinP)), 1f / Scalars.PI, 0.5f)
		);
	}

	/// <summary>
	/// Calculates and outputs the sines and cosines of theta and phi based on <paramref name="uv"/>.
	/// NOTE: cosine phi can be easily calculated by using <see cref="FastMath.Identity"/>.
	/// </summary>
	static void ToThetaPhi(Float2 uv, out float sinT, out float sinP, out float cosT)
	{
		sinT = uv.x;
		sinP = uv.y;

		float sign = 1f;

		if (sinT > 1.5f) //If sinT is out of domain, it means that cosT should be negative
		{
			sinT -= 3f;
			sign = -1f;
		}

		cosT = FastMath.Identity(sinT) * sign;
	}

	static float ProbabilityDensityCone(float cosMaxT) => 1f / Scalars.TAU / (1f - cosMaxT);
}