﻿using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Objects;
using EchoRenderer.Objects.Scenes;

namespace EchoRenderer.Mathematics.Primitives
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
		public int InstanceCount { get; private set; }

		/// <summary>
		/// A stack that holds the different <see cref="PressedInstance.id"/> branches to reach this <see cref="geometry"/>.
		/// </summary>
		fixed uint instances[ObjectPack.MaxLayer];

		/// <summary>
		/// Returns the <see cref="PressedInstance.id"/> of the topmost <see cref="PressedInstance"/>,
		/// which is the one that immediately contains <see cref="geometry"/> in its pack.
		/// </summary>
		public uint FinalInstanceId
		{
			get
			{
				Assert.IsTrue(InstanceCount > 0);
				return instances[InstanceCount - 1];
			}
		}

		/// <summary>
		/// Pushes a new <paramref name="instance"/> onto this <see cref="GeometryToken"/> as a new layer.
		/// </summary>
		public void Push(PressedInstance instance)
		{
			Assert.IsTrue(InstanceCount < ObjectPack.MaxLayer);
			instances[InstanceCount++] = instance.id;
		}

		/// <summary>
		/// Pops a <see cref="PressedInstance"/> from the top of this <see cref="GeometryToken"/>.
		/// </summary>
		public void Pop()
		{
			Assert.IsTrue(InstanceCount > 0);
			--InstanceCount;
		}

		/// <summary>
		/// Applies the local space to world space transform of <see cref="geometry"/> to <paramref name="direction"/>.
		/// </summary>
		public readonly void ApplyWorldTransform(ScenePresser presser, ref Float3 direction)
		{
			for (int i = InstanceCount - 1; i >= 0; i--)
			{
				uint id = instances[i];
				var instance = presser.GetPressedPackInstance(id);
				instance.TransformInverse(ref direction);
			}

			direction = direction.Normalized;
		}

		/// <summary>
		/// Returns whether this <see cref="GeometryToken"/> equals <paramref name="other"/> exactly.
		/// </summary>
		public readonly bool Equals(in GeometryToken other)
		{
			if (geometry != other.geometry || InstanceCount != other.InstanceCount) return false;

			//TODO: create a hash for the token so we can push back sequential comparison one more step

			for (int i = 0; i < InstanceCount; i++)
			{
				if (instances[i] != other.instances[i]) return false;
			}

			return true;
		}
	}
}