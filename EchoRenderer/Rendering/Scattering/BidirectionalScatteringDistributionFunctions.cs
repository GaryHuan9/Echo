using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Intersections;

namespace EchoRenderer.Rendering.Scattering
{
	/// <summary>
	/// A container of many <see cref="BidirectionalDistributionFunction"/>.
	/// </summary>
	public class BidirectionalScatteringDistributionFunctions
	{
		/// <summary>
		/// Resets and initializes this <see cref="BidirectionalScatteringDistributionFunctions"/> for new use.
		/// </summary>
		public void Reset(in HitQuery query, float newEta = 1f)
		{
			//Note that we do not need to worry about releasing the references from the functions
			//because they are supposed to be allocated in an arena, which handles the deallocation

			count = 0;
			eta   = newEta;

			normalShading  = query.shading.normal;
			normalGeometry = query.normal;

			//Use a helper to calculate the tangent and binormal vectors
			Float3 helper = Math.Abs(normalShading.x) >= 0.9f ? Float3.forward : Float3.right;

			tangent  = Float3.Cross(normalShading, helper).Normalized;
			binormal = Float3.Cross(normalShading, tangent).Normalized;
		}

		int   count;
		float eta;

		Float3 normalShading;
		Float3 normalGeometry;

		Float3 tangent;
		Float3 binormal;

		BidirectionalDistributionFunction[] functions = new BidirectionalDistributionFunction[InitialSize];

		const int InitialSize = 16;

		/// <summary>
		/// Adds <paramref name="function"/> into this <see cref="BidirectionalScatteringDistributionFunctions"/>.
		/// </summary>
		public void Add(BidirectionalDistributionFunction function)
		{
			int length = functions.Length;

			if (count == length)
			{
				var array = functions;
				functions = new BidirectionalDistributionFunction[length * 2];
				for (int i = 0; i < length; i++) functions[i] = array[i];
			}

			functions[count++] = function;
		}

		/// <summary>
		/// Counts how many <see cref="BidirectionalScatteringDistributionFunctions"/> in this
		/// <see cref="BidirectionalScatteringDistributionFunctions"/> have <paramref name="type"/>.
		/// </summary>
		public int Count(BidirectionalDistributionFunctionType type)
		{
			int result = 0;

			for (int i = 0; i < count; i++)
			{
				result += functions[i].MatchType(type) ? 0 : 1;
			}

			return result;
		}

		/// <summary>
		/// Samples all <see cref="BidirectionalDistributionFunction"/> that matches with <paramref name="type"/>.
		/// See <see cref="BidirectionalDistributionFunction.Sample(in Float3, in Float3)"/> for more information.
		/// </summary>
		public Float3 Sample(in Float3 outgoingWorld, in Float3 incidentWorld, BidirectionalDistributionFunctionType type)
		{
			Float3 outgoing = WorldToLocal(outgoingWorld);
			Float3 incident = WorldToLocal(incidentWorld);

			//Determines whether this is a reflection or a transmission using geometry normal to avoid light leak
			bool reflect = outgoingWorld.Dot(normalGeometry) * incidentWorld.Dot(normalGeometry) > 0f;

			Float3 result = Float3.zero;

			for (int i = 0; i < count; i++)
			{
				var function = functions[i];

				if (!function.MatchType(type)) continue;
				if (reflect  && function.HasType(BidirectionalDistributionFunctionType.reflection) ||
					!reflect && function.HasType(BidirectionalDistributionFunctionType.transmission)) result += function.Sample(outgoing, incident);
			}

			return result;
		}

		/// <summary>
		/// Returns the aggregated reflectance for all <see cref="BidirectionalDistributionFunction"/> that matches with <paramref name="type"/>.
		/// See <see cref="BidirectionalDistributionFunction.GetReflectance(in Float3, ReadOnlySpan{Float2})"/> for more information.
		/// </summary>
		public Float3 GetReflectance(in Float3 outgoingWorld, ReadOnlySpan<Float2> samples, BidirectionalDistributionFunctionType type)
		{
			Float3 outgoing = WorldToLocal(outgoingWorld);
			Float3 result   = Float3.zero;

			for (int i = 0; i < count; i++)
			{
				var function = functions[i];
				if (!function.MatchType(type)) continue;
				result += function.GetReflectance(outgoing, samples);
			}

			return result;
		}

		/// <summary>
		/// Returns the aggregated reflectance for all <see cref="BidirectionalDistributionFunction"/> that matches with <paramref name="type"/>.
		/// See <see cref="BidirectionalDistributionFunction.GetReflectance(ReadOnlySpan{Float2}, ReadOnlySpan{Float2})"/> for more information.
		/// </summary>
		public Float3 GetReflectance(ReadOnlySpan<Float2> samples0, ReadOnlySpan<Float2> samples1, BidirectionalDistributionFunctionType type)
		{
			Float3 result = Float3.zero;

			for (int i = 0; i < count; i++)
			{
				var function = functions[i];
				if (!function.MatchType(type)) continue;
				result += function.GetReflectance(samples0, samples1);
			}

			return result;
		}

		Float3 WorldToLocal(in Float3 direction) => new(direction.Dot(tangent), direction.Dot(binormal), direction.Dot(normalShading));

		Float3 LocalToWorld(in Float3 direction) => new
		(
			tangent.x * direction.x + binormal.x * direction.y + normalShading.x * direction.z,
			tangent.y * direction.x + binormal.y * direction.y + normalShading.y * direction.z,
			tangent.z * direction.x + binormal.z * direction.y + normalShading.z * direction.z
		);
	}
}