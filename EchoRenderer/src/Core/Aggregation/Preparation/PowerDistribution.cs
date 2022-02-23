using System;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Distributions;

namespace EchoRenderer.Core.Aggregation.Preparation;

public class PowerDistribution
{
	public PowerDistribution() { }

	readonly int[] starts;
	readonly NodeToken[][] partitions;
	readonly Piecewise1 distribution;

	/// <summary>
	/// The total summed power of this <see cref="PowerDistribution"/>.
	/// </summary>
	public float Total => distribution.sum;

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