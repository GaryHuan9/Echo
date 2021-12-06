using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Rendering.Sampling;

namespace EchoRenderer.Rendering.Scattering
{
	/// <summary>
	/// A bidirectional scattering distribution function which is the container for many <see cref="BxDF"/>.
	/// </summary>
	public class BSDF
	{
		/// <summary>
		/// Resets and initializes this <see cref="BSDF"/> for new use.
		/// </summary>
		public void Reset(in Interaction interaction, float newEta = 1f)
		{
			//Note that we do not need to worry about releasing the references from the functions
			//because they are supposed to be allocated in an arena, which handles the deallocation

			count = 0;
			eta = newEta;

			transform = new NormalTransform(interaction.normal);
			geometryNormal = interaction.geometryNormal;
		}

		int count;
		float eta;

		NormalTransform transform;
		Float3 geometryNormal;

		BxDF[] functions = new BxDF[InitialSize];

		const int InitialSize = 8;

		/// <summary>
		/// Adds <paramref name="function"/> into this <see cref="BSDF"/>.
		/// </summary>
		public void Add(BxDF function)
		{
			int length = functions.Length;

			if (count == length)
			{
				BxDF[] array = functions;
				functions = new BxDF[length * 2];
				for (int i = 0; i < length; i++) functions[i] = array[i];
			}

			functions[count++] = function;
		}

		/// <summary>
		/// Counts how many <see cref="BSDF"/> in this
		/// <see cref="BSDF"/> have <paramref name="type"/>.
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
		/// Samples all <see cref="BxDF"/> that matches with <paramref name="type"/>.
		/// See <see cref="BxDF.Sample(in Float3, in Float3)"/> for more information.
		/// </summary>
		public Float3 Sample(in Float3 outgoingWorld, in Float3 incidentWorld, FunctionType type)
		{
			Float3 outgoing = transform.WorldToLocal(outgoingWorld);
			Float3 incident = transform.WorldToLocal(incidentWorld);

			bool reflect = Reflect(outgoingWorld, incidentWorld);

			Float3 sampled = Float3.zero;

			for (int i = 0; i < count; i++)
			{
				BxDF function = functions[i];

				if (!function.MatchType(type)) continue;
				if (reflect && function.HasType(FunctionType.reflection) ||
					!reflect && function.HasType(FunctionType.transmission)) sampled += function.Sample(outgoing, incident);
			}

			return sampled;
		}

		/// <summary>
		/// Samples all <see cref="BxDF"/> that matches with <paramref name="type"/> with an output direction.
		/// See <see cref="BxDF.Sample(in Float3, in Distribution2, out Float3, out float)"/> for more information.
		/// </summary>
		public Float3 Sample(in Float3 outgoingWorld, Distro2 distro, FunctionType type, out Float3 incidentWorld, out float pdf, out FunctionType sampledType)
		{
			int index = FindFunction(type, ref distro, out int matched);

			if (index < 0)
			{
				incidentWorld = Float3.zero;
				pdf = 0f;
				sampledType = FunctionType.none;
				return Float3.zero;
			}

			BxDF selected = functions[index];

			sampledType = selected.functionType;

			//Sample the selected function
			Float3 outgoing = transform.WorldToLocal(outgoingWorld);
			Float3 sampled = selected.Sample(outgoing, distro, out Float3 incident, out pdf);

			if (pdf.AlmostEquals())
			{
				incidentWorld = Float3.zero;
				return Float3.zero;
			}

			incidentWorld = transform.LocalToWorld(incident);

			//Sample the other matching functions
			if (!selected.HasType(FunctionType.specular) && matched > 1)
			{
				bool reflect = Reflect(outgoingWorld, incidentWorld);

				for (int i = 0; i < count; i++)
				{
					BxDF function = functions[i];
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
		/// Returns the aggregated reflectance for all <see cref="BxDF"/> that matches with <paramref name="type"/>.
		/// See <see cref="BxDF.GetReflectance(in Float3, ReadOnlySpan{Distribution2})"/> for more information.
		/// </summary>
		public Float3 GetReflectance(in Float3 outgoingWorld, ReadOnlySpan<Distro2> distros, FunctionType type)
		{
			Float3 outgoing = transform.WorldToLocal(outgoingWorld);
			Float3 reflectance = Float3.zero;

			for (int i = 0; i < count; i++)
			{
				BxDF function = functions[i];
				if (!function.MatchType(type)) continue;
				reflectance += function.GetReflectance(outgoing, distros);
			}

			return reflectance;
		}

		/// <summary>
		/// Returns the aggregated reflectance for all <see cref="BxDF"/> that matches with <paramref name="type"/>.
		/// See <see cref="BxDF.GetReflectance(ReadOnlySpan{Distro2}, ReadOnlySpan{Distro2})"/> for more information.
		/// </summary>
		public Float3 GetReflectance(ReadOnlySpan<Distro2> distros0, ReadOnlySpan<Distro2> distros1, FunctionType type)
		{
			Float3 reflectance = Float3.zero;

			for (int i = 0; i < count; i++)
			{
				BxDF function = functions[i];
				if (!function.MatchType(type)) continue;
				reflectance += function.GetReflectance(distros0, distros1);
			}

			return reflectance;
		}

		/// <summary>
		/// Returns the aggregated probability density for all <see cref="BxDF"/> that matches with
		/// <paramref name="type"/>. See <see cref="BxDF.ProbabilityDensity"/> for more information.
		/// </summary>
		public float ProbabilityDensity(in Float3 outgoingWorld, in Float3 incidentWorld, FunctionType type)
		{
			Float3 outgoing = transform.WorldToLocal(outgoingWorld);
			Float3 incident = transform.WorldToLocal(incidentWorld);

			int matched = 0;
			float pdf = 0f;

			for (int i = 0; i < count; i++)
			{
				BxDF function = functions[i];
				if (!function.MatchType(type)) continue;

				pdf += function.ProbabilityDensity(outgoing, incident);
				++matched;
			}

			return matched == 0 ? 0f : pdf / matched;
		}

		/// <summary>
		/// Determines whether the direction pair <paramref name="outgoingWorld"/> and <paramref name="incidentWorld"/>
		/// is a reflection transport or a transmission transport using our geometry normal to avoid light leak.
		/// </summary>
		bool Reflect(in Float3 outgoingWorld, in Float3 incidentWorld) => outgoingWorld.Dot(geometryNormal) * incidentWorld.Dot(geometryNormal) > 0f;

		int FindFunction(FunctionType type, ref Distro2 distro, out int matched)
		{
			Span<int> stack = stackalloc int[count];

			matched = 0; //The number of functions matching type

			for (int i = 0; i < count; i++)
			{
				if (!functions[i].MatchType(type)) continue;
				stack[matched++] = i;
			}

			if (matched == 0) return -1;

			//Finds the index and remaps sample to a uniformed distribution because we just used it to find a function
			int index = stack[(distro.u.x * matched).Floor()];
			distro = new Distro2(distro.u.x * matched - index, distro.u.y);

			return index;
		}
	}
}