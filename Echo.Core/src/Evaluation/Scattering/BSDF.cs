using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Scattering;

/// <summary>
/// A bidirectional scattering distribution function which is the container for many <see cref="BxDF"/>.
/// </summary>
public class BSDF
{
	/// <summary>
	/// Resets and initializes this <see cref="BSDF"/> for new use.
	/// </summary>
	public void Reset(in Contact contact, float newEta = 1f)
	{
		//Note that we do not need to worry about releasing the references from the functions
		//because they are supposed to be allocated in an arena, which handles the deallocation

		count = 0;
		eta = newEta;

		transform = new NormalTransform(contact.shade.Normal);
		geometricNormal = contact.point.normal;
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
	/// Counts how many <see cref="BxDF"/> in this <see cref="BSDF"/> fits the attributes outlined by <paramref name="type"/>.
	/// </summary>
	public int Count(FunctionType type)
	{
		if (FunctionType.All.Fits(type)) return count;

		int result = 0;

		for (int i = 0; i < count; i++) result += functions[i].type.Fits(type) ? 0 : 1;

		return result;
	}

	/// <summary>
	/// Evaluates all <see cref="BxDF"/> contained in this <see cref="BSDF"/> that matches
	/// <paramref name="type"/>. See <see cref="BxDF.Evaluate"/> for more information.
	/// </summary>
	public RGB128 Evaluate(in Float3 outgoingWorld, in Float3 incidentWorld, FunctionType type = FunctionType.All)
	{
		Float3 outgoing = transform.WorldToLocal(outgoingWorld);
		Float3 incident = transform.WorldToLocal(incidentWorld);

		FunctionType reflect = Reflect(outgoingWorld, incidentWorld);
		var total = RGB128.Black;

		for (int i = 0; i < count; i++)
		{
			BxDF function = functions[i];
			if (!function.type.Fits(type) | !function.type.Any(reflect)) continue;
			total += function.Evaluate(outgoing, incident);
		}

		return total;
	}

	/// <summary>
	/// Returns the aggregated probability density for all <see cref="BxDF"/> that matches with
	/// <paramref name="type"/>. See <see cref="BxDF.ProbabilityDensity"/> for more information.
	/// </summary>
	public float ProbabilityDensity(in Float3 outgoingWorld, in Float3 incidentWorld, FunctionType type = FunctionType.All)
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
	/// Outputs the main <see cref="BxDF"/> sampled to <paramref name="selected"/>.
	/// See <see cref="BxDF.Sample"/> for more information.
	/// </summary>
	public Probable<RGB128> Sample(in Float3 outgoingWorld, Sample2D sample, out Float3 incidentWorld,
								   out BxDF selected, FunctionType type = FunctionType.All)
	{
		//Uniformly select a matching function
		int matched = FindFunction(type, ref Unsafe.AsRef(in sample.x), out int index);

		if (matched == 0)
		{
			incidentWorld = Float3.Zero;
			selected = default;
			return Probable<RGB128>.Impossible;
		}

		selected = functions[index];

		//Sample the selected function
		Float3 outgoing = transform.WorldToLocal(outgoingWorld);
		var sampled = selected.Sample(sample, outgoing, out Float3 incident);

		if (sampled.NotPossible | sampled.content.IsZero)
		{
			incidentWorld = Float3.Zero;
			return Probable<RGB128>.Impossible;
		}

		incidentWorld = transform.LocalToWorld(incident);

		//If there is only one function, we have finished
		if (matched == 1) return sampled;
		Assert.IsTrue(matched > 1);

		//If the selected function is specular, we are also finished
		if (selected.type.Any(FunctionType.Specular))
		{
			//Specular functions are Dirac delta distributions
			Assert.AreEqual(sampled.pdf, 1f);
			return (sampled, 1f / matched);
		}

		//Sample the other matching functions
		FunctionType reflect = Reflect(outgoingWorld, incidentWorld);

		(RGB128 total, float pdf) = sampled;

		for (int i = 0; i < count; i++)
		{
			BxDF function = functions[i];

			if ((function == selected) | !function.type.Fits(type)) continue;
			pdf += function.ProbabilityDensity(outgoing, incident);

			if (function.type.Any(reflect)) total += function.Evaluate(outgoing, incident);
		}

		return (total, pdf / matched);
	}

	/// <summary>
	/// Returns the aggregated reflectance for all <see cref="BxDF"/> that matches with <paramref name="type"/>.
	/// See <see cref="BxDF.GetReflectance(in Float3, ReadOnlySpan{Sample2D})"/> for more information.
	/// </summary>
	public RGB128 GetReflectance(in Float3 outgoingWorld, ReadOnlySpan<Sample2D> samples, FunctionType type)
	{
		Float3 outgoing = transform.WorldToLocal(outgoingWorld);
		var reflectance = RGB128.Black;

		for (int i = 0; i < count; i++)
		{
			BxDF function = functions[i];
			if (!function.type.Fits(type)) continue;
			reflectance += function.GetReflectance(outgoing, samples);
		}

		return reflectance;
	}

	/// <summary>
	/// Returns the aggregated reflectance for all <see cref="BxDF"/> that matches with <paramref name="type"/>.
	/// See <see cref="BxDF.GetReflectance(ReadOnlySpan{Sample2D}, ReadOnlySpan{Sample2D})"/> for more information.
	/// </summary>
	public RGB128 GetReflectance(ReadOnlySpan<Sample2D> samples0, ReadOnlySpan<Sample2D> samples1, FunctionType type)
	{
		var reflectance = RGB128.Black;

		for (int i = 0; i < count; i++)
		{
			BxDF function = functions[i];
			if (!function.type.Fits(type)) continue;
			reflectance += function.GetReflectance(samples0, samples1);
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
		return dot0 * dot1 > 0f ? FunctionType.Reflective : FunctionType.Transmissive;
	}

	int FindFunction(FunctionType type, ref Sample1D sample, out int index)
	{
		if (count == 0)
		{
			index = default;
			return 0;
		}

		//If all stored functions match
		if (FunctionType.All.Fits(type))
		{
			sample = sample.Range(count, out index);
			return count;
		}

		//Count the number of functions matching type
		Span<int> stack = stackalloc int[count];

		int matched = 0;

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

		//Select one function
		sample = sample.Range(matched, out index);
		index = stack[index];

		return matched;
	}
}