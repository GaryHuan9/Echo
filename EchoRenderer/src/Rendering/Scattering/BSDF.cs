using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Rendering.Distributions;

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

			transform = new NormalTransform(interaction.shade.Normal);
			geometricNormal = interaction.point.normal;
		}

		int count;
		float eta;

		NormalTransform transform;
		Float3 geometricNormal;

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
		/// Returns the total number of <see cref="BxDF"/> included int his <see cref="BSDF"/>.
		/// </summary>
		public int Count() => count;

		/// <summary>
		/// Counts how many <see cref="BxDF"/> in this <see cref="BSDF"/> fits the attributes outlined by <paramref name="type"/>.
		/// </summary>
		public int Count(FunctionType type)
		{
			int result = 0;

			for (int i = 0; i < count; i++)
			{
				result += functions[i].type.Fits(type) ? 0 : 1;
			}

			return result;
		}

		/// <summary>
		/// Evaluates all <see cref="BxDF"/> contained in this <see cref="BSDF"/> that matches
		/// <paramref name="type"/>. See <see cref="BxDF.Evaluate"/> for more information.
		/// </summary>
		public Float3 Evaluate(in Float3 outgoingWorld, in Float3 incidentWorld, FunctionType type = FunctionType.all)
		{
			Float3 outgoing = transform.WorldToLocal(outgoingWorld);
			Float3 incident = transform.WorldToLocal(incidentWorld);

			Float3 evaluation = Float3.zero;
			FunctionType reflect = Reflect(outgoingWorld, incidentWorld);

			for (int i = 0; i < count; i++)
			{
				BxDF function = functions[i];
				if (!function.type.Fits(type) || !function.type.Any(reflect)) continue;
				evaluation += function.Evaluate(outgoing, incident);
			}

			return evaluation;
		}

		/// <summary>
		/// Returns the aggregated probability density for all <see cref="BxDF"/> that matches with
		/// <paramref name="type"/>. See <see cref="BxDF.ProbabilityDensity"/> for more information.
		/// </summary>
		public float ProbabilityDensity(in Float3 outgoingWorld, in Float3 incidentWorld, FunctionType type = FunctionType.all)
		{
			Float3 outgoing = transform.WorldToLocal(outgoingWorld);
			Float3 incident = transform.WorldToLocal(incidentWorld);

			int matched = 0;
			float pdf = 0f;

			for (int i = 0; i < count; i++)
			{
				BxDF function = functions[i];
				if (!function.type.Fits(type)) continue;

				pdf += function.ProbabilityDensity(outgoing, incident);
				++matched;
			}

			return matched < 2 ? pdf : pdf / matched;
		}

		/// <summary>
		/// Samples all <see cref="BxDF"/> that matches <paramref name="type"/>.
		/// See <see cref="BxDF.Sample"/> for more information.
		/// </summary>
		public Float3 Sample(in Float3 outgoingWorld, Distro2 distro,
							 out Float3 incidentWorld, out float pdf,
							 out FunctionType sampledType, FunctionType type = FunctionType.all)
		{
			//Uniformly select a matching function
			Distro1 distroFind = distro.x;
			int matched = FindFunction(type, ref distroFind, out int index);

			if (matched == 0)
			{
				incidentWorld = Float3.zero;
				pdf = 0f;
				sampledType = FunctionType.none;
				return Float3.zero;
			}

			distro = new Distro2(distroFind, distro.y);

			BxDF selected = functions[index];
			sampledType = selected.type;

			//Sample the selected function
			Float3 outgoing = transform.WorldToLocal(outgoingWorld);
			Float3 value = selected.Sample(outgoing, distro, out Float3 incident, out pdf);

			if (!FastMath.Positive(pdf))
			{
				incidentWorld = Float3.zero;
				return Float3.zero;
			}

			incidentWorld = transform.LocalToWorld(incident);

			//If there is only one function, we have finished
			if (matched == 1) return value;
			Assert.IsTrue(matched > 1);

			//If the selected function is specular, we are also finished
			if (selected.type.Any(FunctionType.specular))
			{
				Assert.AreEqual(pdf, 1f); //This is because specular functions are Dirac delta distributions

				pdf /= matched;
				return value;
			}

			//Sample the other matching functions
			FunctionType reflect = Reflect(outgoingWorld, incidentWorld);

			for (int i = 0; i < count; i++)
			{
				BxDF function = functions[i];

				if (function == selected || !function.type.Fits(type)) continue;
				pdf += function.ProbabilityDensity(outgoing, incident);

				if (function.type.Any(reflect)) value += function.Evaluate(outgoing, incident);
			}

			if (matched > 1) pdf /= matched;
			return value;
		}

		/// <summary>
		/// Returns the aggregated reflectance for all <see cref="BxDF"/> that matches with <paramref name="type"/>.
		/// See <see cref="BxDF.GetReflectance(in Float3, ReadOnlySpan{Distro2})"/> for more information.
		/// </summary>
		public Float3 GetReflectance(in Float3 outgoingWorld, ReadOnlySpan<Distro2> distros, FunctionType type)
		{
			Float3 outgoing = transform.WorldToLocal(outgoingWorld);
			Float3 reflectance = Float3.zero;

			for (int i = 0; i < count; i++)
			{
				BxDF function = functions[i];
				if (!function.type.Fits(type)) continue;
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
				if (!function.type.Fits(type)) continue;
				reflectance += function.GetReflectance(distros0, distros1);
			}

			return reflectance;
		}

		/// <summary>
		/// Determines whether the direction pair <paramref name="outgoingWorld"/> and <paramref name="incidentWorld"/>
		/// is a reflective transport or a transmissive transport using our geometry normal to avoid light leak.
		/// </summary>
		FunctionType Reflect(in Float3 outgoingWorld, in Float3 incidentWorld)
		{
			float dot0 = outgoingWorld.Dot(geometricNormal);
			float dot1 = incidentWorld.Dot(geometricNormal);

			//Returns based on whether the two directions are on the same side of the normal
			return dot0 * dot1 > 0f ? FunctionType.reflective : FunctionType.transmissive;
		}

		int FindFunction(FunctionType type, ref Distro1 distro, out int index)
		{
			if (count == 0)
			{
				index = default;
				return 0;
			}

			int matched; //The number of functions matching type

			if (FunctionType.all.Fits(type))
			{
				//All stored functions match
				matched = count;
				index = distro.Range(matched);
			}
			else
			{
				//Count the number of matched and select
				Span<int> stack = stackalloc int[count];

				matched = 0;

				for (int i = 0; i < count; i++)
				{
					if (!functions[i].type.Fits(type)) continue;
					stack[matched++] = i;
				}

				if (matched == 0)
				{
					index = default;
					return 0;
				}

				index = stack[distro.Range(matched)];
			}

			//Remaps distro by "zooming in" because we just used it to find index
			distro = (Distro1)FastMath.FMA(distro.u, matched, -index);

			return matched;
		}
	}
}