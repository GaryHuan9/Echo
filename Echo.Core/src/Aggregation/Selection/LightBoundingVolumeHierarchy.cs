using System;
using System.Collections.Generic;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Distributions;

namespace Echo.Core.Aggregation.Selection;

public class LightBoundingVolumeHierarchy : LightPicker
{
	public LightBoundingVolumeHierarchy(View<Tokenized<LightBounds>> boundsView)
	{
		root = Build(boundsView);
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

	public override ConeBounds ConeBounds => root.bounds.cone;
	public override float Power => root.bounds.power;

	public override AxisAlignedBoundingBox GetTransformedBounds(in Float4x4 transform) => new(stackalloc AxisAlignedBoundingBox[1] { root.bounds.aabb }, transform);

	public override Probable<EntityToken> Pick(in GeometryPoint origin, ref Sample1D sample) => Pick(origin, ref sample, root, 1f);

	public override float ProbabilityMass(EntityToken token, in GeometryPoint origin)
	{
		if (!map.TryGetValue(token, out ulong branches)) return 0f;
		return ProbabilityMass(origin, root, branches);
	}

	static Node Build(View<Tokenized<LightBounds>> boundsView)
	{
		if (boundsView.Length == 1) return new Node(boundsView[0].content, boundsView[0].token);

		int length = boundsView.Length;

		AxisAlignedBoundingBox parentAabb = boundsView[0].content.aabb;

		foreach (ref readonly var pair in boundsView) parentAabb = parentAabb.Encapsulate(pair.content.aabb);

		int majorAxis = parentAabb.MajorAxis;
		boundsView.AsSpan().Sort((pair0, pair1) =>
		{
			float center0 = pair0.content.aabb.Center[majorAxis];
			float center1 = pair1.content.aabb.Center[majorAxis];
			return center0.CompareTo(center1);
		});

		float[] costs = new float[length];
		LightBounds lightBounds = boundsView[^1].content;

		for (int i = length - 2; i >= 0; i--)
		{
			costs[i + 1] = lightBounds.Area;
			lightBounds = lightBounds.Encapsulate(boundsView[i].content);
		}

		float minCost = float.PositiveInfinity;
		int minIndex = -1;

		lightBounds = boundsView[0].content;

		for (int i = 1; i < length; i++)
		{
			float cost = costs[i] + lightBounds.Area;

			if (cost < minCost)
			{
				minCost = cost;
				minIndex = i;
			}

			lightBounds = lightBounds.Encapsulate(boundsView[i].content);
		}

		return new Node
		(
			Build(boundsView[minIndex..]),
			Build(boundsView[..minIndex])
		);
	}

	static Probable<EntityToken> Pick(in GeometryPoint origin, ref Sample1D sample, Node node, float pdf)
	{
		if (node.child0 == null) return new Probable<EntityToken>(node.token, pdf);

		float importance0 = node.child0.bounds.Importance(origin);
		float importance1 = node.child1.bounds.Importance(origin);

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

		float importance0 = node.child0.bounds.Importance(origin);
		float importance1 = node.child1.bounds.Importance(origin);
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

			bounds = child0.bounds.Encapsulate(child1.bounds);
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