using System;
using System.Collections.Generic;

namespace ForceRenderer.Mathematics
{
	/// <summary>
	/// Builds the branch of a <see cref="BoundingVolumeHierarchy"/>.
	/// </summary>
	public class BranchBuilder
	{
		public BranchBuilder(IReadOnlyList<AxisAlignedBoundingBox> aabbs, int[] sourceIndices)
		{
			this.aabbs = aabbs;
			this.sourceIndices = sourceIndices;

			cutTailVolumes = new AxisAlignedBoundingBox[sourceIndices.Length];
		}

		readonly IReadOnlyList<AxisAlignedBoundingBox> aabbs;
		readonly int[] sourceIndices;

		readonly AxisAlignedBoundingBox[] cutTailVolumes;
		public int NodeCount { get; private set; }

		public Node Build()
		{
			if (NodeCount != 0) throw new Exception("Branch already built!");

			if (sourceIndices.Length > 1)
			{
				int axis = new AxisAlignedBoundingBox(aabbs).extend.MaxIndex;

				SortIndices(sourceIndices, axis);
				return BuildLayer(sourceIndices);
			}

			NodeCount++;
			return new Node(aabbs[0], sourceIndices[0]);
		}

		Node BuildLayer(ReadOnlySpan<int> indices)
		{
			PrepareCutTailVolumes(indices);

			int minIndex = SearchSurfaceAreaHeuristics(indices, out var headVolume, out var tailVolume);
			AxisAlignedBoundingBox aabb = headVolume.Encapsulate(tailVolume);

			int axis = aabb.extend.MaxIndex;

			//Recursively construct deeper layers
			Node child0 = BuildChild(indices[..minIndex], headVolume, axis);
			Node child1 = BuildChild(indices[minIndex..], tailVolume, axis);

			NodeCount++;
			return new Node(child0, child1, aabb);
		}

		Node BuildChild(ReadOnlySpan<int> indices, in AxisAlignedBoundingBox aabb, int parentAxis)
		{
			if (indices.Length == 1)
			{
				NodeCount++;
				return new Node(aabb, indices[0]);
			}

			int axis = aabb.extend.MaxIndex;
			if (axis == parentAxis) return BuildLayer(indices);

			int[] childIndices = indices.ToArray();

			SortIndices(childIndices, axis);
			return BuildLayer(childIndices);
		}

		/// <summary>
		/// Sorts <paramref name="indices"/> based on each aabb's location on <paramref name="axis"/>.
		/// </summary>
		void SortIndices(int[] indices, int axis)
		{
			Comparison<int> comparison = axis switch
										 {
											 0 => (index0, index1) => aabbs[index0].center.x.CompareTo(aabbs[index1].center.x),
											 1 => (index0, index1) => aabbs[index0].center.y.CompareTo(aabbs[index1].center.y),
											 _ => (index0, index1) => aabbs[index0].center.z.CompareTo(aabbs[index1].center.z)
										 };

			Array.Sort(indices, comparison);
		}

		/// <summary>
		/// Calculates all of the tail volumes and stores them to <see cref="cutTailVolumes"/>.
		/// The prepared data is then used by <see cref="SearchSurfaceAreaHeuristics"/>.
		/// </summary>
		void PrepareCutTailVolumes(ReadOnlySpan<int> indices)
		{
			AxisAlignedBoundingBox cutTailVolume = aabbs[indices[^1]];

			for (int i = indices.Length - 2; i >= 0; i--)
			{
				cutTailVolumes[i + 1] = cutTailVolume;
				cutTailVolume = cutTailVolume.Encapsulate(aabbs[indices[i]]);
			}
		}

		/// <summary>
		/// Searches the length of <paramref name="indices"/> to find and return the spot where the SAH is the lowest.
		/// Also returns the two volumes after cutting at the returned index. NOTE: Uses the prepared <see cref="cutTailVolumes"/>.
		/// </summary>
		int SearchSurfaceAreaHeuristics(ReadOnlySpan<int> indices, out AxisAlignedBoundingBox headVolume, out AxisAlignedBoundingBox tailVolume)
		{
			AxisAlignedBoundingBox cutHeadVolume = aabbs[indices[0]];

			float minCost = float.MaxValue;
			int minIndex = -1;

			headVolume = default;
			tailVolume = default;

			for (int i = 1; i < indices.Length; i++)
			{
				AxisAlignedBoundingBox cutTailVolume = cutTailVolumes[i];
				float cost = cutHeadVolume.Area * i + cutTailVolume.Area * (indices.Length - i);

				if (cost < minCost)
				{
					minCost = cost;
					minIndex = i;

					headVolume = cutHeadVolume;
					tailVolume = cutTailVolume;
				}

				cutHeadVolume = cutHeadVolume.Encapsulate(aabbs[indices[i]]);
			}

			return minIndex;
		}

		/// <summary>
		/// Linked node used when constructing bvh
		/// </summary>
		public class Node
		{
			public Node(Node child0, Node child1, AxisAlignedBoundingBox aabb)
			{
				this.child0 = child0;
				this.child1 = child1;
				this.aabb = aabb;
			}

			public Node(AxisAlignedBoundingBox aabb, int index)
			{
				this.aabb = aabb;
				this.index = index;
			}

			public readonly Node child0;
			public readonly Node child1;

			public readonly AxisAlignedBoundingBox aabb;
			public readonly int index;

			public bool IsLeaf => child0 == null || child1 == null;
		}
	}
}