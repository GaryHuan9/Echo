using System;
using System.Collections.Generic;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Threads;
using EchoRenderer.Mathematics.Primitives;

namespace EchoRenderer.Mathematics.Intersections
{
	/// <summary>
	/// Builds the branch of a <see cref="BoundingVolumeHierarchy"/>.
	/// </summary>
	public class BranchBuilder
	{
		public BranchBuilder(IReadOnlyList<AxisAlignedBoundingBox> aabbs) => this.aabbs = aabbs;

		readonly IReadOnlyList<AxisAlignedBoundingBox> aabbs;
		readonly IComparer<int>[] boxComparers = new IComparer<int>[3];

		AxisAlignedBoundingBox[] cutTailVolumes;

		int _nodeCount;

		public int NodeCount => InterlockedHelper.Read(ref _nodeCount);
		const int ParallelBuildThreshold = 4096; //We can increase this value to disable parallel building

		/// <summary>
		/// Initialize build on the root builder.
		/// </summary>
		public Node Build(Span<int> indices)
		{
			if (NodeCount != 0) throw new Exception("Branch already built!");
			if (aabbs.Count != indices.Length) throw new Exception("Non-root builder!");

			if (indices.Length > 1)
			{
				int axis = new AxisAlignedBoundingBox(aabbs).MajorAxis;

				SortIndices(indices, axis);
				return BuildLayer(indices);
			}

			Interlocked.Increment(ref _nodeCount);
			return new Node(aabbs[indices[0]], indices[0]);
		}

		Node BuildLayer(ReadOnlySpan<int> indices)
		{
			PrepareCutTailVolumes(indices);

			int minIndex = SearchSurfaceAreaHeuristics(indices, out var headVolume, out var tailVolume);
			AxisAlignedBoundingBox aabb = headVolume.Encapsulate(tailVolume);

			int axis = aabb.MajorAxis;

			//Recursively construct deeper layers
			Node child0;
			Node child1;

			if (indices.Length < ParallelBuildThreshold)
			{
				child0 = BuildChild(indices[..minIndex], headVolume, axis);
				child1 = BuildChild(indices[minIndex..], tailVolume, axis);
			}
			else if (minIndex > indices.Length / 2)
			{
				var builder = BuildChildParallel(indices[..minIndex], headVolume, axis);
				child0 = BuildChild(indices[minIndex..], tailVolume, axis);
				child1 = builder.Wait();
			}
			else
			{
				var builder = BuildChildParallel(indices[minIndex..], tailVolume, axis);
				child0 = BuildChild(indices[..minIndex], headVolume, axis);
				child1 = builder.Wait();
			}

			//Places the child with the larger surface area first to improve branch prediction
			if (headVolume.Area < tailVolume.Area) CodeHelper.Swap(ref child0, ref child1);

			Interlocked.Increment(ref _nodeCount);
			return new Node(child0, child1, aabb, axis);
		}

		NodeBuilder BuildChildParallel(ReadOnlySpan<int> indices, in AxisAlignedBoundingBox aabb, int parentAxis)
		{
			int axis = aabb.MajorAxis;
			int[] childIndices = indices.ToArray();

			if (axis != parentAxis) SortIndices(childIndices, axis);
			return new NodeBuilder(this, childIndices);
		}

		Node BuildChild(ReadOnlySpan<int> indices, in AxisAlignedBoundingBox aabb, int parentAxis)
		{
			if (indices.Length == 1)
			{
				Interlocked.Increment(ref _nodeCount);
				return new Node(aabb, indices[0]);
			}

			int axis = aabb.MajorAxis;
			if (axis == parentAxis) return BuildLayer(indices);

			Span<int> childIndices = indices.ToArray();

			SortIndices(childIndices, axis);
			return BuildLayer(childIndices);
		}

		/// <summary>
		/// Sorts <paramref name="indices"/> based on each aabb's location on <paramref name="axis"/>.
		/// </summary>
		void SortIndices(Span<int> indices, int axis)
		{
			ref IComparer<int> comparer = ref boxComparers[axis];

			comparer ??= axis switch
						 {
							 0 => new BoxComparerX(aabbs),
							 1 => new BoxComparerY(aabbs),
							 _ => new BoxComparerZ(aabbs)
						 };

			indices.Sort(comparer);
		}

		/// <summary>
		/// Calculates all of the tail volumes and stores them to <see cref="cutTailVolumes"/>.
		/// The prepared data is then used by <see cref="SearchSurfaceAreaHeuristics"/>.
		/// </summary>
		void PrepareCutTailVolumes(ReadOnlySpan<int> indices)
		{
			cutTailVolumes ??= new AxisAlignedBoundingBox[indices.Length];
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
			public Node(Node child0, Node child1, AxisAlignedBoundingBox aabb, int axis)
			{
				this.child0 = child0;
				this.child1 = child1;
				this.aabb = aabb;
				this.axis = axis;
			}

			public Node(AxisAlignedBoundingBox aabb, int index)
			{
				this.aabb = aabb;
				this.index = index;
			}

			public readonly Node child0;
			public readonly Node child1;

			public readonly AxisAlignedBoundingBox aabb;
			public readonly int index; //If is leaf, this indicates the index of the token
			public readonly int axis;  //The axis used to divide the two children in this node

			public bool IsLeaf => child0 == null || child1 == null;
		}

		class NodeBuilder
		{
			public NodeBuilder(BranchBuilder source, int[] indices)
			{
				this.source = source;
				this.indices = indices;

				thread = new Thread(Build);
				thread.Start();
			}

			Node result;

			readonly BranchBuilder source;
			readonly int[] indices;
			readonly Thread thread;

			public Node Wait()
			{
				thread.Join();
				return InterlockedHelper.Read(ref result);
			}

			void Build()
			{
				BranchBuilder builder = new BranchBuilder(source.aabbs);

				Interlocked.Exchange(ref result, builder.BuildLayer(indices));
				Interlocked.Add(ref source._nodeCount, builder.NodeCount);
			}
		}

		class BoxComparerX : IComparer<int>
		{
			public BoxComparerX(IReadOnlyList<AxisAlignedBoundingBox> aabbs) => this.aabbs = aabbs;

			readonly IReadOnlyList<AxisAlignedBoundingBox> aabbs;

			public int Compare(int index0, int index1)
			{
				AxisAlignedBoundingBox box0 = aabbs[index0];
				AxisAlignedBoundingBox box1 = aabbs[index1];

				return (box0.min.x + box0.max.x).CompareTo(box1.min.x + box1.max.x);
			}
		}

		class BoxComparerY : IComparer<int>
		{
			public BoxComparerY(IReadOnlyList<AxisAlignedBoundingBox> aabbs) => this.aabbs = aabbs;

			readonly IReadOnlyList<AxisAlignedBoundingBox> aabbs;

			public int Compare(int index0, int index1)
			{
				AxisAlignedBoundingBox box0 = aabbs[index0];
				AxisAlignedBoundingBox box1 = aabbs[index1];

				return (box0.min.y + box0.max.y).CompareTo(box1.min.y + box1.max.y);
			}
		}

		class BoxComparerZ : IComparer<int>
		{
			public BoxComparerZ(IReadOnlyList<AxisAlignedBoundingBox> aabbs) => this.aabbs = aabbs;

			readonly IReadOnlyList<AxisAlignedBoundingBox> aabbs;

			public int Compare(int index0, int index1)
			{
				AxisAlignedBoundingBox box0 = aabbs[index0];
				AxisAlignedBoundingBox box1 = aabbs[index1];

				return (box0.min.z + box0.max.z).CompareTo(box1.min.z + box1.max.z);
			}
		}
	}
}