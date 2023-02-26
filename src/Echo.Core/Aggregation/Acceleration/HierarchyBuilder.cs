using System;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Memory;

namespace Echo.Core.Aggregation.Acceleration;

public abstract class HierarchyBuilder
{
	public HierarchyBuilder(View<Tokenized<BoxBound>> bounds) => this.bounds = bounds;

	protected readonly View<Tokenized<BoxBound>> bounds;

	public abstract Node Build();

	public sealed class Node
	{
		public Node(in BoxBound bound, Node child0, Node child1, int axis)
		{
			Ensure.IsNotNull(child0);
			Ensure.IsNotNull(child1);

			this.bound = bound;

			nodeCount = child0.nodeCount + child1.nodeCount + 1;
			nodeDepth = Math.Max(child0.nodeDepth, child1.nodeDepth) + 1;

			_child0 = child0;
			_child1 = child1;
			_axis = axis;
		}

		public Node(in Tokenized<BoxBound> tokenized) : this(tokenized.content, tokenized.token) { }

		public Node(in BoxBound bound, EntityToken token)
		{
			this.bound = bound;
			nodeCount = 1;
			nodeDepth = 1;
			_token = token;
		}

		public readonly BoxBound bound;
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
				Ensure.IsFalse(IsLeaf);
				return _child0;
			}
		}

		public Node Child1
		{
			get
			{
				Ensure.IsFalse(IsLeaf);
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
				Ensure.IsFalse(IsLeaf);
				return _axis;
			}
		}

		readonly EntityToken _token;

		public EntityToken Token
		{
			get
			{
				Ensure.IsTrue(IsLeaf);
				return _token;
			}
		}
	}
}