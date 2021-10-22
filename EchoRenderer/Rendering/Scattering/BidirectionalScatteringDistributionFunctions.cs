using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Rendering.Sampling;

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
			eta = newEta;

			normalShading = query.shading.normal;
			normalGeometry = query.normal;

			//Use a helper to calculate the tangent and binormal vectors
			Float3 helper = Math.Abs(normalShading.x) >= 0.9f ? Float3.forward : Float3.right;

			tangent = Float3.Cross(normalShading, helper).Normalized;
			binormal = Float3.Cross(normalShading, tangent).Normalized;
		}

		int   count;
		float eta;

		Float3 normalShading;
		Float3 normalGeometry;

		Float3 tangent;
		Float3 binormal;

		BidirectionalDistributionFunction[] functions = new BidirectionalDistributionFunction[InitialSize];

		const int InitialSize = 8;

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
		public int Count(FunctionType type)
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
		public Float3 Sample(in Float3 outgoingWorld, in Float3 incidentWorld, FunctionType type)
		{
			Float3 outgoing = WorldToLocal(outgoingWorld);
			Float3 incident = WorldToLocal(incidentWorld);

			bool reflect = Reflect(outgoingWorld, incidentWorld);

			Float3 sampled = Float3.zero;

			for (int i = 0; i < count; i++)
			{
				var function = functions[i];

				if (!function.MatchType(type)) continue;
				if (reflect && function.HasType(FunctionType.reflection) ||
					!reflect && function.HasType(FunctionType.transmission)) sampled += function.Sample(outgoing, incident);
			}

			return sampled;
		}

		/// <summary>
		/// Samples all <see cref="BidirectionalDistributionFunction"/> that matches with <paramref name="type"/> with an output direction.
		/// See <see cref="BidirectionalDistributionFunction.Sample(in Float3, out Float3, in Sample2, out float)"/> for more information.
		/// </summary>
		public Float3 Sample(in Float3 outgoingWorld, out Float3 incidentWorld, Sample2 sample, out float pdf, FunctionType type, out FunctionType sampledType)
		{
			int index = FindFunction(type, sample, out int matched);

			if (index < 0)
			{
				pdf = 0f;
				sampledType = FunctionType.none;
				incidentWorld = Float3.zero;
				return Float3.zero;
			}

			//Remap sample back to a uniformed distribution because we just used it to find a function
			sample = new Sample2(sample.X * matched - index, sample.Y);
			BidirectionalDistributionFunction selected = functions[index];

			sampledType = selected.functionType;

			//Sample the selected function
			Float3 outgoing = WorldToLocal(outgoingWorld);
			Float3 sampled = selected.Sample(outgoing, out Float3 incident, sample, out pdf);

			if (pdf.AlmostEquals())
			{
				incidentWorld = Float3.zero;
				return Float3.zero;
			}

			incidentWorld = LocalToWorld(incident);

			//Sample the other matching functions
			if (!selected.HasType(FunctionType.specular) && matched > 1)
			{
				bool reflect = Reflect(outgoingWorld, incidentWorld);

				for (int i = 0; i < count; i++)
				{
					BidirectionalDistributionFunction function = functions[i];
					if (function == selected || !function.MatchType(type)) continue;

					pdf += function.ProbabilityDensity(outgoing, incident);
					if (reflect && function.HasType(FunctionType.reflection) ||
						!reflect && function.HasType(FunctionType.transmission)) sampled += function.Sample(outgoing, incident);
				}
			}

			pdf /= matched;
			return sampled;
		}

		/// <summary>
		/// Returns the aggregated reflectance for all <see cref="BidirectionalDistributionFunction"/> that matches with <paramref name="type"/>.
		/// See <see cref="BidirectionalDistributionFunction.GetReflectance(in Float3, ReadOnlySpan{Sample2})"/> for more information.
		/// </summary>
		public Float3 GetReflectance(in Float3 outgoingWorld, ReadOnlySpan<Sample2> samples, FunctionType type)
		{
			Float3 outgoing = WorldToLocal(outgoingWorld);
			Float3 reflectance = Float3.zero;

			for (int i = 0; i < count; i++)
			{
				var function = functions[i];
				if (!function.MatchType(type)) continue;
				reflectance += function.GetReflectance(outgoing, samples);
			}

			return reflectance;
		}

		/// <summary>
		/// Returns the aggregated reflectance for all <see cref="BidirectionalDistributionFunction"/> that matches with <paramref name="type"/>.
		/// See <see cref="BidirectionalDistributionFunction.GetReflectance(ReadOnlySpan{Sample2}, ReadOnlySpan{Sample2})"/> for more information.
		/// </summary>
		public Float3 GetReflectance(ReadOnlySpan<Sample2> samples0, ReadOnlySpan<Sample2> samples1, FunctionType type)
		{
			Float3 reflectance = Float3.zero;

			for (int i = 0; i < count; i++)
			{
				var function = functions[i];
				if (!function.MatchType(type)) continue;
				reflectance += function.GetReflectance(samples0, samples1);
			}

			return reflectance;
		}

		/// <summary>
		/// Returns the aggregated probability density for all <see cref="BidirectionalDistributionFunction"/> that matches with
		/// <paramref name="type"/>. See <see cref="BidirectionalDistributionFunction.ProbabilityDensity"/> for more information.
		/// </summary>
		public float ProbabilityDensity(in Float3 outgoingWorld, in Float3 incidentWorld, FunctionType type)
		{
			Float3 outgoing = WorldToLocal(outgoingWorld);
			Float3 incident = WorldToLocal(incidentWorld);

			int matched = 0;
			float pdf = 0f;

			for (int i = 0; i < count; i++)
			{
				var function = functions[i];
				if (!function.MatchType(type)) continue;

				pdf += function.ProbabilityDensity(outgoing, incident);
				++matched;
			}

			return matched == 0 ? 0f : pdf / matched;
		}

		Float3 WorldToLocal(in Float3 direction) => new(direction.Dot(tangent), direction.Dot(binormal), direction.Dot(normalShading));

		Float3 LocalToWorld(in Float3 direction) => new
		(
			tangent.x * direction.x + binormal.x * direction.y + normalShading.x * direction.z,
			tangent.y * direction.x + binormal.y * direction.y + normalShading.y * direction.z,
			tangent.z * direction.x + binormal.z * direction.y + normalShading.z * direction.z
		);

		/// <summary>
		/// Determines whether the direction pair <paramref name="outgoingWorld"/> and <paramref name="incidentWorld"/>
		/// is a reflection transport or a transmission transport using our geometry normal to avoid light leak.
		/// </summary>
		bool Reflect(in Float3 outgoingWorld, in Float3 incidentWorld) => outgoingWorld.Dot(normalGeometry) * incidentWorld.Dot(normalGeometry) > 0f;

		int FindFunction(FunctionType type, Sample2 sample, out int matched)
		{
			Span<int> stack = stackalloc int[count];

			matched = 0; //The number of functions matching type

			for (int i = 0; i < count; i++)
			{
				if (!functions[i].MatchType(type)) continue;
				stack[matched++] = i;
			}

			if (matched == 0) return -1;
			float stretched = sample.X * matched;
			return stack[stretched.Floor()];
		}
	}
}