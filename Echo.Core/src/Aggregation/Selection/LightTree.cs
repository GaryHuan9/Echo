using System;
using System.Collections.Generic;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Sampling;

namespace Echo.Core.Aggregation.Selection;

/// <summary>
/// An implementation of <see cref="LightPicker"/>.
/// </summary>
/// <remarks>This class is currently awfully unoptimized and messy. However it is functional, and we
/// are running out of time for the deadline. Thus optimization is delayed until the future. </remarks>
public class LightTree : LightPicker
{
	public LightTree(View<Tokenized<LightBound>> bounds)
	{
		root = Build(bounds);
		AddToMap(root, 0, 0ul);

		void AddToMap(Node node, int depth, ulong branches)
		{
			if (node == null) return;
			Assert.IsTrue(depth < 64);

			if (node.child0 == null) map.Add(node.token, branches);
			else
			{
				AddToMap(node.child0, depth + 1, branches);
				AddToMap(node.child1, depth + 1, branches | (1ul << depth));
			}
		}
	}

	readonly Node root;
	readonly Dictionary<EntityToken, ulong> map = new();

	public override ConeBound ConeBound => root.bound.cone;
	public override float Power => root == null ? default : root.bound.power;

	public override BoxBound GetTransformedBounds(in Float4x4 transform) => new(stackalloc BoxBound[1] { root.bound.box }, transform);

	public override Probable<EntityToken> Pick(in GeometryPoint origin, ref Sample1D sample) => Pick(origin, ref sample, root, 1f);

	public override float ProbabilityMass(EntityToken token, in GeometryPoint origin)
	{
		if (!map.TryGetValue(token, out ulong branches)) return 0f;
		return ProbabilityMass(origin, root, branches);
	}

	static Node Build(View<Tokenized<LightBound>> bounds)
	{
		if (bounds.Length == 1) return new Node(bounds[0].content, bounds[0].token);
		if (bounds.Length == 0) return null;

		int length = bounds.Length;

		BoxBound parentBound = bounds[0].content.box;

		foreach (ref readonly var pair in bounds) parentBound = parentBound.Encapsulate(pair.content.box);

		int majorAxis = parentBound.MajorAxis;
		bounds.AsSpan().Sort((pair0, pair1) =>
		{
			float center0 = pair0.content.box.Center[majorAxis];
			float center1 = pair1.content.box.Center[majorAxis];
			return center0.CompareTo(center1);
		});

		float[] costs = new float[length];
		LightBound lightBound = bounds[^1].content;

		for (int i = length - 2; i >= 0; i--)
		{
			costs[i + 1] = lightBound.Area;
			lightBound = lightBound.Encapsulate(bounds[i].content);
		}

		float minCost = float.PositiveInfinity;
		int minIndex = -1;

		lightBound = bounds[0].content;

		for (int i = 1; i < length; i++)
		{
			float cost = costs[i] + lightBound.Area;

			if (cost < minCost)
			{
				minCost = cost;
				minIndex = i;
			}

			lightBound = lightBound.Encapsulate(bounds[i].content);
		}

		return new Node
		(
			Build(bounds[minIndex..]),
			Build(bounds[..minIndex])
		);
	}

	static Probable<EntityToken> Pick(in GeometryPoint origin, ref Sample1D sample, Node node, float pdf)
	{
		if (node.child0 == null) return new Probable<EntityToken>(node.token, pdf);

		float importance0 = node.child0.bound.Importance(origin);
		float importance1 = node.child1.bound.Importance(origin);

		if (!FastMath.Positive(importance0) && !FastMath.Positive(importance1)) return Probable<EntityToken>.Impossible;

		float split = importance0 / (importance0 + importance1);

		if (sample < split)
		{
			sample = sample.Stretch(0f, split);
			return Pick(origin, ref sample, node.child0, pdf * split);
		}

		sample = sample.Stretch(split, 1f);
		return Pick(origin, ref sample, node.child1, pdf * (1f - split));
	}

	static float ProbabilityMass(in GeometryPoint origin, Node node, ulong branches)
	{
		if (node.child0 == null)
		{
			Assert.AreEqual(branches, 0ul);
			return 1f;
		}

		float importance0 = node.child0.bound.Importance(origin);
		float importance1 = node.child1.bound.Importance(origin);
		float split = importance0 / (importance0 + importance1);

		if ((branches & 1) == 0)
		{
			return split * ProbabilityMass(origin, node.child0, branches >> 1);
		}

		return (1f - split) * ProbabilityMass(origin, node.child1, branches >> 1);
	}

	class Node
	{
		public Node(Node child0, Node child1)
		{
			this.child0 = child0;
			this.child1 = child1;

			bound = child0.bound.Encapsulate(child1.bound);
		}

		public Node(LightBound bound, EntityToken token)
		{
			this.bound = bound;
			this.token = token;
		}

		public readonly Node child0;
		public readonly Node child1;

		public readonly LightBound bound;
		public readonly EntityToken token;
	}
}