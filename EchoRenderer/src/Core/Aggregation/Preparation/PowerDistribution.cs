﻿using System;
using CodeHelpers.Diagnostics;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Distributions;

namespace EchoRenderer.Core.Aggregation.Preparation;

/// <summary>
/// A distribution of <see cref="NodeToken"/>s created from emissive power value of the objects they represent.
/// </summary>
public class PowerDistribution
{
	public PowerDistribution(ReadOnlySpan<float> powerValues, ReadOnlySpan<int> segments, NodeTokenArray tokenArray)
	{
		distribution = new Piecewise1(powerValues);

		int partitionLength = segments.Length;

		partitions = new ReadOnlyView<NodeToken>[partitionLength];
		starts = new int[partitionLength];

		//Calculate partition starts using rolling sum
		int rolling = 0;

		for (int i = 0; i < partitionLength; i++)
		{
			var partition = tokenArray[segments[i]];
			int length = partition.Length;

			Assert.IsTrue(length > 0);
			partitions[i] = partition;

			starts[i] = rolling;
			rolling += length;
		}

		Assert.AreEqual(rolling, powerValues.Length);
	}

	readonly Piecewise1 distribution;

	readonly int[] starts;
	readonly ReadOnlyView<NodeToken>[] partitions;

	/// <summary>
	/// The total summed power of this <see cref="PowerDistribution"/>.
	/// </summary>
	public float Total => distribution.sum;

	/// <summary>
	/// Samples one <see cref="NodeToken"/> from this <see cref="PowerDistribution"/>
	/// based on <paramref name="distro"/> and outputs <paramref name="pdf"/>.
	/// </summary>
	public NodeToken Sample(Distro1 distro, out float pdf)
	{
		//Sample from piecewise and binary search
		int index = distribution.SampleDiscrete(distro, out pdf);
		int partitionIndex = starts.AsSpan().BinarySearch(index);

		//Find token from index
		if (partitionIndex < 0) partitionIndex = ~partitionIndex - 1;
		return partitions[partitionIndex][index - starts[index]];
	}
}