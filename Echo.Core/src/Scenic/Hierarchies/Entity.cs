using System;
using System.Collections;
using System.Collections.Generic;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Common;

namespace Echo.Core.Scenic.Hierarchies;

/// <summary>
/// The base for any object that can exist in a <see cref="Scene"/>.
/// </summary>
public class Entity : IEnumerable<Entity>
{
	public Entity() => Root = this as EntityPack;

	readonly List<Entity> children = new();
	bool transformDirty;

	/// <summary>
	/// The <see cref="Entity"/> at a higher hierarchy level that contains this <see cref="Entity"/>.
	/// </summary>
	/// <remarks>Null if no <see cref="Entity"/> ever invoked <see cref="Add"/> on this <see cref="Entity"/> yet.</remarks>
	public Entity Parent { get; private set; }

	/// <summary>
	/// The <see cref="EntityPack"/> which is at the root of this <see cref="Entity"/>'s hierarchy.
	/// </summary>
	/// <remarks>Null if the root of this <see cref="Entity"/>'s hierarchy is not yet an <see cref="EntityPack"/>.</remarks>
	public EntityPack Root { get; private set; }

	Float3 _position;
	Float3 _rotation;
	float _scale = 1f;

	/// <summary>
	/// The position of this <see cref="Entity"/> relative to its <see cref="Parent"/>.
	/// </summary>
	public virtual Float3 Position
	{
		get => _position;
		set
		{
			if (_position == value) return;
			_position = value;
			DirtyTransform();
		}
	}

	/// <summary>
	/// The rotation (in euler angles) of this <see cref="Entity"/> relative to its <see cref="Parent"/>.
	/// </summary>
	public virtual Float3 Rotation
	{
		get => _rotation;
		set
		{
			if (_rotation == value) return;
			_rotation = value;
			DirtyTransform();
		}
	}

	/// <summary>
	/// The scale of this <see cref="Entity"/> relative to its <see cref="Parent"/>.
	/// </summary>
	/// <remarks>This must be a positive value.</remarks>
	public virtual float Scale
	{
		get => _scale;
		set
		{
			if (value <= 0f) throw new SceneException($"The {nameof(Scale)} of an {nameof(Entity)} must be positive.");
			if (_scale.AlmostEquals(value)) return;

			_scale = value;
			DirtyTransform();
		}
	}

	Float4x4 _forwardTransform = Float4x4.identity;
	Float4x4 _inverseTransform = Float4x4.identity;

	/// <summary>
	/// A <see cref="Float4x4"/> that transforms from the space of the <see cref="EntityPack"/>
	/// that contains this <see cref="Entity"/> to the space of this <see cref="Entity"/>.
	/// </summary>
	public Float4x4 ForwardTransform
	{
		get
		{
			RecalculateTransform();
			return _forwardTransform;
		}
	}

	/// <summary>
	/// A <see cref="Float4x4"/> that transforms from the space of this <see cref="Entity"/>
	/// to the space of the <see cref="EntityPack"/> that contains this <see cref="Entity"/>.
	/// </summary>
	public Float4x4 InverseTransform
	{
		get
		{
			RecalculateTransform();
			return _inverseTransform;
		}
	}

	/// <summary>
	/// The <see cref="Position"/> of this <see cref="Entity"/> relative to its containing <see cref="EntityPack"/>.
	/// </summary>
	public Float3 ContainedPosition => InverseTransform.GetColumn(3).XYZ;

	/// <summary>
	/// The <see cref="Rotation"/> of this <see cref="Entity"/> relative to its containing <see cref="EntityPack"/>.
	/// </summary>
	public Float3x3 ContainedRotation
	{
		get
		{
			float scaleR = 1f / ContainedScale;
			Float4x4 transform = InverseTransform;

			return new Float3x3
			(
				transform.f00 * scaleR, transform.f01 * scaleR, transform.f02 * scaleR,
				transform.f10 * scaleR, transform.f11 * scaleR, transform.f12 * scaleR,
				transform.f20 * scaleR, transform.f21 * scaleR, transform.f22 * scaleR
			);
		}
	}

	/// <summary>
	/// The <see cref="Scale"/> of this <see cref="Entity"/> relative to its containing <see cref="EntityPack"/>.
	/// </summary>
	public float ContainedScale => Utility.GetScale(InverseTransform);

	/// <summary>
	/// Adds an <see cref="Entity"/> as a child.
	/// </summary>
	/// <param name="child">The <see cref="Entity"/> to add.</param>
	/// <exception cref="ArgumentException">If <paramref name="child"/> has already been added to a <see cref="Parent"/>.</exception>
	/// <remarks>Once successfully added, <paramref name="child"/> will inherit the transform of this <see cref="Entity"/>.</remarks>
	public void Add(Entity child)
	{
		if (child.Parent == this) throw new SceneException($"Cannot add a child to the same {nameof(Parent)} twice.");
		if (child.Parent != null) throw new SceneException($"Cannot move a child to a different {nameof(Parent)}.");

		child.CheckParent(this);
		AddImpl(child);
	}

	/// <summary>
	/// Enumerates through all of the children of this <see cref="Entity"/>.
	/// </summary>
	/// <param name="recursive">Whether to also include and yield the children's children and so on.</param>
	/// <param name="self">Whether to include this <see cref="Entity"/> in ths result as well.</param>
	/// <returns>An <see cref="IEnumerable{T}"/> that enables this enumeration.</returns>
	public IEnumerable<Entity> LoopChildren(bool recursive = false, bool self = false)
	{
		if (self) yield return this;

		foreach (Entity directChild in children)
		{
			yield return directChild;
			if (!recursive) continue;

			foreach (Entity child in directChild.LoopChildren(true)) yield return child;
		}
	}

	/// <summary>
	/// Checks whether an <see cref="Entity"/> can be set as this <see cref="Entity"/>'s <see cref="Parent"/>.
	/// </summary>
	/// <param name="parent">The <see cref="Entity"/> that is potentially going to be the <see cref="Parent"/>.</param>
	/// <exception cref="SceneException">Thrown if the input <see cref="Entity"/> cannot be our <see cref="Parent"/>.</exception>
	protected virtual void CheckParent(Entity parent) { }

	/// <summary>
	/// Checks whether an <see cref="EntityPack"/> can be set as this <see cref="Entity"/>'s <see cref="Root"/>.
	/// </summary>
	/// <param name="root">The <see cref="EntityPack"/> that is potentially going to be the <see cref="Root"/>.</param>
	/// <exception cref="SceneException">Thrown if the input <see cref="Entity"/> cannot be our <see cref="Root"/>.</exception>
	protected virtual void CheckRoot(EntityPack root) { }

	/// <summary>
	/// The actual implementation of <see cref="Add"/>.
	/// </summary>
	/// <param name="child">The <see cref="Entity"/> to add as a child.</param>
	protected virtual void AddImpl(Entity child)
	{
		children.Add(child);
		child.Parent = this;
		child.SetRoot(Root);
		child.DirtyTransform();
	}

	/// <summary>
	/// Sets the <see cref="Root"/> of this <see cref="Entity"/>.
	/// </summary>
	/// <param name="root">The <see cref="EntityPack"/> to set as <see cref="Root"/>.</param>
	void SetRoot(EntityPack root)
	{
		if (Root == root) return;
		if (root == null) throw new ArgumentNullException(nameof(root));
		if (Root != null) throw new InvalidOperationException("Cannot change the root once it is set.");

		CheckRoot(root);
		Root = root;

		for (int i = 0; i < children.Count; i++) children[i].SetRoot(root);
	}

	IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => children.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<Entity>)this).GetEnumerator();

	void RecalculateTransform()
	{
		if (!transformDirty) return;
		transformDirty = false;

		Float4x4 transform = Float4x4.Transformation(Position, Rotation, (Float3)Scale);

		if (Parent == null)
		{
			_forwardTransform = transform.Inverse;
			_inverseTransform = transform;
		}
		else
		{
			Parent.RecalculateTransform();

			_forwardTransform = Parent.ForwardTransform * transform.Inverse;
			_inverseTransform = Parent.InverseTransform * transform;
		}
	}

	void DirtyTransform()
	{
		if (transformDirty) return;
		transformDirty = true;

		for (int i = 0; i < children.Count; i++) children[i].DirtyTransform();
	}
}