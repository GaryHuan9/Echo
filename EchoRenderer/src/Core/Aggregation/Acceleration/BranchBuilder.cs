using System;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Common.Memory;

namespace EchoRenderer.Core.Aggregation.Acceleration;

/// <summary>
/// Builds the branch of a <see cref="BoundingVolumeHierarchy"/>.
/// </summary>
public class BranchBuilder
{
	public BranchBuilder(ReadOnlyView<AxisAlignedBoundingBox> aabbs) : this(aabbs, aabbs.Length) { }

	BranchBuilder(ReadOnlyView<AxisAlignedBoundingBox> aabbs, int capacity)
	{
		this.aabbs = aabbs;
		this.capacity = capacity;
		sorter = new BoxSorter(capacity);
	}

	readonly ReadOnlyView<AxisAlignedBoundingBox> aabbs;
	readonly int capacity;
	readonly BoxSorter sorter;

	AxisAlignedBoundingBox[] cutTailVolumes;

	const int ParallelBuildThreshold = 4096; //We can increase this value to disable parallel building

	/// <summary>
	/// Initialize build on the root builder.
	/// </summary>
	public Node Build(View<int> indices)
	{
		Assert.IsTrue(indices.Length <= capacity);
		LayerData data = new LayerData(aabbs, indices);

		if (data.aabbs.Length != data.Length) throw new Exception("Not a root builder!");
		if (data.Length == 1) return new Node(data[0], data.indices[0]);

		int axis = new AxisAlignedBoundingBox(data.aabbs).MajorAxis;

		SortIndices(data, axis);
		return BuildLayer(data);
	}

	Node BuildLayer(in LayerData data)
	{
		Assert.IsFalse(data.Length < 2);
		PrepareCutTailVolumes(data);

		int minIndex = SearchSurfaceAreaHeuristics(data, out var headVolume, out var tailVolume);
		AxisAlignedBoundingBox aabb = headVolume.Encapsulate(tailVolume);

		int axis = aabb.MajorAxis;

		//Split data based on minIndex; headData is always larger than tailData
		LayerData headData;
		LayerData tailData;

		if (minIndex > data.Length / 2)
		{
			headData = data[..minIndex];
			tailData = data[minIndex..];
		}
		else
		{
			headData = data[minIndex..];
			tailData = data[..minIndex];

			CodeHelper.Swap(ref headVolume, ref tailVolume);
		}

		//Recursively construct deeper layers
		Node child0;
		Node child1;

		if (headData.Length < ParallelBuildThreshold)
		{
			child0 = BuildChild(headData, headVolume, axis);
			child1 = BuildChild(tailData, tailVolume, axis);
		}
		else
		{
			var builder = BuildChildParallel(headData, headVolume, axis);
			child1 = BuildChild(tailData, tailVolume, axis);
			child0 = builder.WaitForNode();
		}

		//Places the child with the larger surface area first to improve branch prediction
		if (headVolume.Area < tailVolume.Area) CodeHelper.Swap(ref child0, ref child1);

		return new Node(child0, child1, aabb, axis);
	}

	Node BuildChild(in LayerData data, in AxisAlignedBoundingBox aabb, int parentAxis)
	{
		if (data.Length == 1) return new Node(aabb, data.indices[0]);

		int axis = aabb.MajorAxis;
		if (axis != parentAxis) SortIndices(data, axis);
		return BuildLayer(data);
	}

	LayerBuilder BuildChildParallel(in LayerData data, in AxisAlignedBoundingBox aabb, int parentAxis)
	{
		Assert.IsTrue(data.Length > 1);

		int axis = aabb.MajorAxis;
		if (axis == parentAxis) axis = -1; //No need to sort because it is already sorted
		return new LayerBuilder(this, data.indices, axis);
	}

	/// <summary>
	/// Sorts the <see cref="LayerData.indices"/> of <paramref name="data"/> based on its
	/// <see cref="LayerData.aabbs"/>'s individual locations on <paramref name="axis"/>.
	/// </summary>
	void SortIndices(in LayerData data, int axis) => sorter.Sort(data.aabbs, data.indices, axis);

	/// <summary>
	/// Calculates all of the tail volumes and stores them to <see cref="cutTailVolumes"/>.
	/// The prepared data is then used by <see cref="SearchSurfaceAreaHeuristics"/>.
	/// </summary>
	void PrepareCutTailVolumes(in LayerData data)
	{
		int length = data.Length;

		cutTailVolumes ??= new AxisAlignedBoundingBox[length];
		AxisAlignedBoundingBox cutTailVolume = data[^1];

		for (int i = length - 2; i >= 0; i--)
		{
			cutTailVolumes[i + 1] = cutTailVolume;
			cutTailVolume = cutTailVolume.Encapsulate(data[i]);
		}
	}

	/// <summary>
	/// Searches the length of <paramref name="data"/> to find and return the spot where the SAH is the lowest.
	/// Also returns the two volumes after cutting at the returned index. NOTE: Uses the prepared <see cref="cutTailVolumes"/>.
	/// </summary>
	int SearchSurfaceAreaHeuristics(in LayerData data, out AxisAlignedBoundingBox headVolume, out AxisAlignedBoundingBox tailVolume)
	{
		AxisAlignedBoundingBox cutHeadVolume = data[0];

		float minCost = float.MaxValue;
		int minIndex = -1;

		headVolume = default;
		tailVolume = default;

		int length = data.indices.Length;

		for (int i = 1; i < length; i++)
		{
			ref readonly AxisAlignedBoundingBox cutTailVolume = ref cutTailVolumes[i];
			float cost = cutHeadVolume.Area * i + cutTailVolume.Area * (length - i);

			if (cost < minCost)
			{
				minCost = cost;
				minIndex = i;

				headVolume = cutHeadVolume;
				tailVolume = cutTailVolume;
			}

			cutHeadVolume = cutHeadVolume.Encapsulate(data[i]);
		}

		return minIndex;
	}

	/// <summary>
	/// A binary linked node that contains two children; contains the result of the branch construction.
	/// </summary>
	public class Node
	{
		public Node(Node child0, Node child1, in AxisAlignedBoundingBox aabb, int axis)
		{
			this.child0 = child0;
			this.child1 = child1;
			this.aabb = aabb;
			this.axis = axis;
		}

		public Node(in AxisAlignedBoundingBox aabb, int index)
		{
			this.aabb = aabb;
			this.index = index;
		}

		public readonly Node child0;
		public readonly Node child1;

		public readonly AxisAlignedBoundingBox aabb;
		public readonly int index; //If is leaf, this indicates the index of the token
		public readonly int axis;  //The axis used to divide the two children in this node

		public bool IsLeaf => (child0 == null) | (child1 == null);
	}

	class LayerBuilder
	{
		public LayerBuilder(BranchBuilder parent, View<int> indices, int sortAxis)
		{
			this.parent = parent;
			this.indices = indices;
			this.sortAxis = sortAxis;

			buildTask = Task.Run(Build);
		}

		readonly Task<Node> buildTask;
		readonly BranchBuilder parent;
		readonly View<int> indices;
		readonly int sortAxis;

		public Node WaitForNode() => buildTask.Result;

		Node Build()
		{
			ReadOnlyView<AxisAlignedBoundingBox> aabbs = parent.aabbs;

			var data = new LayerData(aabbs, indices);
			var builder = new BranchBuilder(aabbs, data.Length);

			//Sort indices if requested by parent
			if (sortAxis >= 0) builder.SortIndices(data, sortAxis);

			return builder.BuildLayer(data);
		}
	}

	readonly ref struct LayerData
	{
		public LayerData(ReadOnlySpan<AxisAlignedBoundingBox> aabbs, View<int> indices)
		{
			this.aabbs = aabbs;
			this.indices = indices;
		}

		LayerData(in LayerData source, Range range)
		{
			aabbs = source.aabbs;
			indices = source.indices[range];
		}

		public readonly ReadOnlySpan<AxisAlignedBoundingBox> aabbs;
		public readonly View<int> indices;

		public int Length => indices.Length;

		public ref readonly AxisAlignedBoundingBox this[Index index] => ref aabbs[indices[index]];

		public LayerData this[Range range] => new(this, range);
	}
}