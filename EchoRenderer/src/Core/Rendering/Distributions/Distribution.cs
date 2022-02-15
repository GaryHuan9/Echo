﻿using System;
using System.Collections.Generic;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics.Randomization;

namespace EchoRenderer.Core.Rendering.Distributions;

public abstract class Distribution
{
	protected Distribution(int sampleCount)
	{
		this.sampleCount = sampleCount;

		arrayOnes = new List<SpanAggregate<Distro1>>();
		arrayTwos = new List<SpanAggregate<Distro2>>();
	}

	protected Distribution(Distribution distribution)
	{
		sampleCount = distribution.sampleCount;

		arrayOnes = Clone(distribution.arrayOnes);
		arrayTwos = Clone(distribution.arrayTwos);

		static List<SpanAggregate<T>> Clone<T>(IReadOnlyList<SpanAggregate<T>> list)
		{
			int count = list.Count;

			List<SpanAggregate<T>> clone = new List<SpanAggregate<T>>(count);
			for (int i = 0; i < count; i++) clone.Add(list[i].Replicate());

			return clone;
		}
	}

	/// <summary>
	/// The maximum number of samples performed for one pixel.
	/// </summary>
	protected readonly int sampleCount;

	protected readonly List<SpanAggregate<Distro1>> arrayOnes;
	protected readonly List<SpanAggregate<Distro2>> arrayTwos;

	int arrayOneIndex;
	int arrayTwoIndex;

	/// <summary>
	/// The position of the current processing pixel.
	/// </summary>
	protected Int2 PixelPosition { get; private set; }

	/// <summary>
	/// The index of the current processing sample.
	/// </summary>
	protected int SampleIndex { get; private set; }

	/// <summary>
	/// The specific pseudo random number generator associated with this <see cref="Distribution"/>.
	/// </summary>
	public IRandom Random { get; set; }

	/// <summary>
	/// Requests a span of one dimensional values with <paramref name="length"/> to be available.
	/// </summary>
	public void RequestSpanOne(int length) => arrayOnes.Add(new SpanAggregate<Distro1>(length, sampleCount));

	/// <summary>
	/// Requests a span of two dimensional values with <paramref name="length"/> to be available.
	/// </summary>
	public void RequestSpanTwo(int length) => arrayTwos.Add(new SpanAggregate<Distro2>(length, sampleCount));

	/// <summary>
	/// Begins sampling on a new pixel at <paramref name="position"/>.
	/// </summary>
	public virtual void BeginPixel(Int2 position)
	{
		SampleIndex = -1;
		PixelPosition = position;
	}

	/// <summary>
	/// Begins a new sample on the current pixel.
	/// </summary>
	public virtual void BeginSample()
	{
		++SampleIndex;
		Assert.IsTrue(SampleIndex < sampleCount);

		arrayOneIndex = -1;
		arrayTwoIndex = -1;
	}

	/// <summary>
	/// Returns the next one dimensional value of this sample.
	/// </summary>
	public abstract Distro1 NextOne();

	/// <summary>
	/// Returns the next two dimensional value of this sample.
	/// </summary>
	public abstract Distro2 NextTwo();

	/// <summary>
	/// Returns the next span of one dimensional values of this sample.
	/// </summary>
	public ReadOnlySpan<Distro1> NextSpanOne() => ++arrayOneIndex < arrayOnes.Count ? arrayOnes[arrayOneIndex][SampleIndex] : Span<Distro1>.Empty;

	/// <summary>
	/// Returns the next span of two dimensional values of this sample.
	/// </summary>
	public ReadOnlySpan<Distro2> NextSpanTwo() => ++arrayTwoIndex < arrayTwos.Count ? arrayTwos[arrayTwoIndex][SampleIndex] : Span<Distro2>.Empty;

	/// <summary>
	/// Produces and returns another copy of this <see cref="Distribution"/> of the same <see cref="Object.GetType()"/> to be used for other threads.
	/// NOTE: Only the information indicated prior to rendering needs to be cloned over; pixel, sample, or PRNG specific data can be ignored.
	/// </summary>
	public abstract Distribution Replicate();

	protected readonly struct SpanAggregate<T>
	{
		public SpanAggregate(int length, int sampleCount)
		{
			this.length = length;
			array = new T[length * sampleCount];
		}

		SpanAggregate(int length, T[] array)
		{
			this.length = length;
			this.array = array;
		}

		public readonly int length;
		public readonly T[] array;

		public Span<T> this[int sampleIndex] => array.AsSpan(sampleIndex * length, length);

		public SpanAggregate<T> Replicate() => new(length, new T[array.Length]);
	}
}