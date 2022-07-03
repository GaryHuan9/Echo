using System;
using CodeHelpers.Diagnostics;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Memory;

namespace Echo.Core.Aggregation.Acceleration;

public abstract class HierarchyBuilder
{
	public HierarchyBuilder(View<Tokenized<AxisAlignedBoundingBox>> boundsView) => this.boundsView = boundsView;

	protected readonly View<Tokenized<AxisAlignedBoundingBox>> boundsView;

	public abstract Node Build();

	public sealed class Node
	{
		public Node(in AxisAlignedBoundingBox aabb, Node child0, Node child1, int axis)
		{
			Assert.IsNotNull(child0);
			Assert.IsNotNull(child1);

			this.aabb = aabb;

			nodeCount = child0.nodeCount + child1.nodeCount + 1;
			nodeDepth = Math.Max(child0.nodeDepth, child1.nodeDepth) + 1;

			_child0 = child0;
			_child1 = child1;
			_axis = axis;
		}

		public Node(in Tokenized<AxisAlignedBoundingBox> tokenized) : this(tokenized.content, tokenized.token) { }

		public Node(in AxisAlignedBoundingBox aabb, EntityToken token)
		{
			this.aabb = aabb;
			nodeCount = 1;
			nodeDepth = 1;
			_token = token;
		}

		public readonly AxisAlignedBoundingBox aabb;
		public readonly uint nodeCount;
		public readonly uint nodeDepth;

		public bool IsLeaf => _child0 == null;

		readonly Node _child0;
		readonly Node _child1;
		readonly int _axis;

		public Node Child0
		{
			get
			{
				Assert.IsFalse(IsLeaf);
				return _child0;
			}
		}

		public Node Child1
		{
			get
			{
				Assert.IsFalse(IsLeaf);
				return _child1;
			}
		}

		/// <summary>
		/// The axis (0 = X, 1 = Y, 2 = Z) used to divide the two children of this node.
		/// </summary>
		public int Axis
		{
			get
			{
				Assert.IsFalse(IsLeaf);
				return _axis;
			}
		}

		readonly EntityToken _token;

		public EntityToken Token
		{
			get
			{
				Assert.IsTrue(IsLeaf);
				return _token;
			}
		}
	}
}