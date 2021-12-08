using System;
using CodeHelpers.Diagnostics;
using EchoRenderer.Mathematics.Accelerators;
using EchoRenderer.Objects;

namespace EchoRenderer.Mathematics.Intersections
{
	/// <summary>
	/// Represents a unique geometry in the pressed scene. Note that this uniqueness transcends object instancing.
	/// </summary>
	public unsafe struct GeometryToken
	{
		/// <summary>
		/// The unique token for one geometry inside a <see cref="PressedPack"/>.
		/// </summary>
		public uint geometry;

		/// <summary>
		/// The number of instances stored in <see cref="instances"/>.
		/// </summary>
		int instanceCount;

		/// <summary>
		/// A stack that holds the different <see cref="PressedInstance.id"/> branches to reach this <see cref="geometry"/>.
		/// </summary>
		fixed uint instances[ObjectPack.MaxLayer];

		/// <summary>
		/// Returns the <see cref="PressedInstance.id"/> of the topmost <see cref="PressedInstance"/>,
		/// which is the one that immediately contains <see cref="geometry"/> in its pack.
		/// </summary>
		public uint FinalInstanceId => instances[instanceCount - 1];

		/// <summary>
		/// Pushes a new <paramref name="instance"/> onto this <see cref="GeometryToken"/> as a new layer.
		/// </summary>
		public void Push(PressedInstance instance)
		{
			Assert.IsTrue(instanceCount < ObjectPack.MaxLayer);
			instances[instanceCount++] = instance.id;
		}

		/// <summary>
		/// Pops a <see cref="PressedInstance"/> from the top of this <see cref="GeometryToken"/>.
		/// </summary>
		public void Pop()
		{
			Assert.IsTrue(instanceCount > 0);
			--instanceCount;
		}

		/// <summary>
		/// Fills <paramref name="span"/> with all the id of all the <see cref="PressedInstance"/> that is required to reach this <see cref="geometry"/>.
		/// </summary>
		public readonly int FillInstanceIds(Span<uint> span)
		{
			Assert.IsTrue(span.Length >= instanceCount);
			for (int i = 0; i < instanceCount; i++)
			{
				span[i] = instances[i];
			}

			return instanceCount;
		}

		/// <summary>
		/// Returns whether this <see cref="GeometryToken"/> equals <paramref name="other"/> exactly.
		/// </summary>
		public readonly bool Equals(in GeometryToken other)
		{
			if (geometry != other.geometry || instanceCount != other.instanceCount) return false;

			//TODO: create a hash for the token so we can push back sequential comparison one more step

			for (int i = 0; i < instanceCount; i++)
			{
				if (instances[i] != other.instances[i]) return false;
			}

			return true;
		}
	}
}