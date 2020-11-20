using System;
using System.Collections.Generic;
using ForceRenderer.Renderers;

namespace ForceRenderer.Mathematics
{
	public class BoundingVolumeHierarchy
	{
		public BoundingVolumeHierarchy(PressedScene pressed, IReadOnlyList<AxisAlignedBoundingBox> boundingBoxes, IReadOnlyList<int> tokens)
		{
			this.pressed = pressed;
		}

		public readonly PressedScene pressed;

		/// <summary>
		/// Traverses and finds the closest intersection of <paramref name="ray"/> with this BVH.
		/// Returns the distance of ths intersection if found. <see cref="float.PositiveInfinity"/> otherwise.
		/// </summary>
		public float GetIntersection(in Ray ray, out int token)
		{

		}

		readonly struct Node
		{
			public Node(int token) : this() => this.token = token;

			public Node(AxisAlignedBoundingBox aabb, int childIndex0, int childIndex1) : this()
			{
				this.aabb = aabb;

				this.childIndex0 = childIndex0;
				this.childIndex1 = childIndex1;
			}

			public readonly int token; //Token will only be assigned if is leaf
			public readonly AxisAlignedBoundingBox aabb;

			public readonly int childIndex0;
			public readonly int childIndex1;

			public bool IsLeaf => childIndex0 == 0;
		}
	}
}