﻿using System;
using System.Runtime.CompilerServices;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
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
	public void Reset(in Contact contact, RGB128 newTint)
	{
		//Note that we do not need to worry about releasing the references from the functions
		//because they are supposed to be allocated in an arena, which handles the deallocation

		count = 0;
		tint = newTint;

		transform = new OrthonormalTransform(contact.shade.Normal);
		geometricNormal = contact.point.normal;
	}

	int count;
	RGB128 tint;

	OrthonormalTransform transform;
	Float3 geometricNormal;

	BxDF[] functions = new BxDF[InitialSize];

	const int InitialSize = 8;

	/// <summary>
	/// The total number of <see cref="BxDF"/> contained in this <see cref="BSDF"/>.
	/// </summary>
	public int Count => count;

	/// <summary>
	/// The number of <see cref="FunctionType.Specular"/> type <see cref="BxDF"/> in this <see cref="BSDF"/>.
	/// </summary>
	public int CountSpecular
	{
		get
		{
			int result = 0;

			for (int i = 0; i < count; i++) result += functions[i].type.Any(FunctionType.Specular) ? 1 : 0;

			return result;
		}
	}

	/// <summary>
	/// Adds a new <see cref="BxDF"/> to this <see cref="BSDF"/>.
	/// </summary>
	/// <param name="allocator">The <see cref="Allocator"/> to use to create the new <see cref="BxDF"/>.</param>
	/// <typeparam name="T">The type of <see cref="BxDF"/> to create.</typeparam>
	/// <returns>The newly created <see cref="BxDF"/>.</returns>
	public T Add<T>(Allocator allocator) where T : BxDF, new()
	{
		T function = allocator.New<T>();
		Add(function);
		return function;
	}

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
	/// Evaluates all <see cref="BxDF"/> contained in this <see cref="BSDF"/> that matches
	/// <paramref name="type"/>. See <see cref="BxDF.Evaluate"/> for more information.
	/// </summary>
	public RGB128 Evaluate(Float3 outgoingWorld, Float3 incidentWorld, FunctionType type = FunctionType.All)
	{
		Ensure.AreEqual(outgoingWorld.SquaredMagnitude, 1f);
		Ensure.AreEqual(incidentWorld.SquaredMagnitude, 1f);

		Float3 outgoing = transform.ApplyInverse(outgoingWorld);
		Float3 incident = transform.ApplyInverse(incidentWorld);
		FunctionType reflect = Reflect(outgoingWorld, incidentWorld);

		var total = RGB128.Black;

		for (int i = 0; i < count; i++)
		{
			BxDF function = functions[i];
			if (!function.type.Fits(type) || !function.type.Any(reflect)) continue;
			total += function.Evaluate(outgoing, incident);
		}

		return tint * total;
	}

	/// <summary>
	/// Returns the aggregated probability density for all <see cref="BxDF"/> that matches with
	/// <paramref name="type"/>. See <see cref="BxDF.ProbabilityDensity"/> for more information.
	/// </summary>
	public float ProbabilityDensity(Float3 outgoingWorld, Float3 incidentWorld, FunctionType type = FunctionType.All)
	{
		Ensure.AreEqual(outgoingWorld.SquaredMagnitude, 1f);
		Ensure.AreEqual(incidentWorld.SquaredMagnitude, 1f);

		Float3 outgoing = transform.ApplyInverse(outgoingWorld);
		Float3 incident = transform.ApplyInverse(incidentWorld);

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
	public Probable<RGB128> Sample(Float3 outgoingWorld, Sample2D sample, out Float3 incidentWorld,
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
		Float3 outgoing = transform.ApplyInverse(outgoingWorld);
		var sampled = selected.Sample(sample, outgoing, out Float3 incident);

		if (sampled.NotPossible || sampled.content.IsZero)
		{
			incidentWorld = Float3.Zero;
			return Probable<RGB128>.Impossible;
		}

		Ensure.AreEqual(incident.SquaredMagnitude, 1f);
		incidentWorld = transform.ApplyForward(incident);
		FunctionType reflect = Reflect(outgoingWorld, incidentWorld);

		//If there is only one function, we have finished
		//If the selected function is specular, we are also finished, since they are are Dirac delta distributions
		if (matched == 1 || selected.type.Any(FunctionType.Specular))
		{
			//Check if shading normal is too extreme and we sampled on the wrong side of the surface
			bool wrongSide = !selected.type.Any(reflect);
			if (wrongSide) return Probable<RGB128>.Impossible;
			return (tint * sampled, sampled.pdf / matched);
		}

		//Sample the other matching functions
		(RGB128 total, float pdf) = sampled;

		for (int i = 0; i < count; i++)
		{
			BxDF function = functions[i];
			if (function == selected) continue;

			if (!function.type.Fits(type) || !function.type.Any(reflect)) continue;

			total += function.Evaluate(outgoing, incident);
			pdf += function.ProbabilityDensity(outgoing, incident);
		}

		return (tint * total, pdf / matched);
	}

	/// <summary>
	/// Determines whether the direction pair <paramref name="outgoingWorld"/> and <paramref name="incidentWorld"/>
	/// is a reflective transport or a transmissive transport using our geometry normal to avoid light leak.
	/// </summary>
	FunctionType Reflect(Float3 outgoingWorld, Float3 incidentWorld)
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