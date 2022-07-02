using System;
using System.Linq;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Distributions;

namespace Echo.Core.Aggregation.Selection;

public class LightBoundingVolumeHierarchy : LightPicker
{
	public LightBoundingVolumeHierarchy(ReadOnlyView<LightBounds> lights, ReadOnlySpan<EntityToken> tokens) =>
		root = Build(Enumerable.Range(0, lights.Length).ToArray(), lights, tokens);

	readonly Node root;

	public override Probable<EntityToken> Pick(Sample1D sample) => throw new NotImplementedException();

	public override float ProbabilityMass(EntityToken token) => throw new NotImplementedException();

	public Probable<EntityToken> Sample(in GeometryPoint origin, Sample1D sample) => Sample(origin, sample, root, 1f);

	static Node Build(Span<int> indices, ReadOnlyView<LightBounds> lights, ReadOnlySpan<EntityToken> tokens)
	{
		if (indices.Length == 1)
		{
			int index = indices[0];
			return new Node(lights[index], tokens[index]);
		}

		int length = lights.Length;

		AxisAlignedBoundingBox parentAabb = lights[0].aabb;

		foreach (ref readonly LightBounds bounds in lights) parentAabb = parentAabb.Encapsulate(bounds.aabb);

		int majorAxis = parentAabb.MajorAxis;
		indices.Sort((index0, index1) =>
		{
			float center0 = lights[index0].aabb.Center[majorAxis];
			float center1 = lights[index1].aabb.Center[majorAxis];
			return center0.CompareTo(center1);
		});

		float[] costs = new float[length];
		LightBounds lightBounds = lights[^1];

		for (int i = length - 2; i >= 0; i--)
		{
			costs[i + 1] = lightBounds.Area;
			lightBounds = lightBounds.Encapsulate(lights[i]);
		}

		float minCost = float.PositiveInfinity;
		int minIndex = -1;

		lightBounds = lights[0];

		for (int i = 1; i < length; i++)
		{
			float cost = costs[i] + lightBounds.Area;

			if (cost < minCost)
			{
				minCost = cost;
				minIndex = i;
			}

			lightBounds = lightBounds.Encapsulate(lights[i]);
		}

		return new Node
		(
			Build(indices[minIndex..], lights, tokens),
			Build(indices[..minIndex], lights, tokens)
		);
	}

	static Probable<EntityToken> Sample(in GeometryPoint origin, Sample1D sample, Node node, float pdf)
	{
		if (node.child0 == null) return new Probable<EntityToken>(node.token, pdf);

		float importance0 = node.child0.bounds.Importance(origin);
		float importance1 = node.child1.bounds.Importance(origin);
		float split = importance0 / (importance0 + importance1);

		if (sample < split)
		{
			sample = sample.Stretch(0f, split);
			return Sample(origin, sample, node.child0, pdf * split);
		}

		sample = sample.Stretch(split, 1f);
		return Sample(origin, sample, node.child1, pdf * (1f - split));
	}

	class Node
	{
		public Node(Node child0, Node child1)
		{
			this.child0 = child0;
			this.child1 = child1;
		}

		public Node(LightBounds bounds, EntityToken token)
		{
			this.bounds = bounds;
			this.token = token;
		}

		public readonly Node child0;
		public readonly Node child1;

		public readonly LightBounds bounds;
		public readonly EntityToken token;
	}
}