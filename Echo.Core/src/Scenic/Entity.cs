using System;
using System.Collections.Generic;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Scenic.Instancing;

namespace Echo.Core.Scenic;

public class Entity
{
	readonly List<Entity> children = new();
	bool transformDirty;

	/// <summary>
	/// The <see cref="Entity"/> that is containing this <see cref="Entity"/>.
	/// </summary>
	public Entity Parent { get; private set; }

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
			if (value <= 0f) throw new ArgumentOutOfRangeException(nameof(value));
			if (_scale.AlmostEquals(value)) return;

			_scale = value;
			DirtyTransform();
		}
	}

	Float4x4 _forwardTransform;
	Float4x4 _inverseTransform;

	/// <summary>
	/// The <see cref="Float4x4"/> transform of this <see cref="Entity"/> relative to its containing <see cref="EntityPack"/>.
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
	/// The <see cref="Float4x4"/> transform of the <see cref="EntityPack"/> that contains
	/// this <see cref="EntityPack"/> relative to the transform of this <see cref="EntityPack"/>.
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
	/// Adds an <see cref="Entity"/> as a child.
	/// </summary>
	/// <param name="child">The <see cref="Entity"/> to add.</param>
	/// <exception cref="ArgumentException">If <paramref name="child"/> has already been added to a <see cref="Parent"/>.</exception>
	/// <remarks>Once successfully added, <paramref name="child"/> will inherit the transform of this <see cref="Entity"/>.</remarks>
	public void Add(Entity child)
	{
		if (child.Parent == this) throw new ArgumentException("Cannot add a child to the same parent twice.", nameof(child));
		if (child.Parent != null) throw new ArgumentException("Cannot move a child to a different parent.", nameof(child));

		Add(child);
		child.Parent = this;
		child.DirtyTransform();
	}

	/// <summary>
	/// Enumerates through all of the children of this <see cref="Entity"/>.
	/// </summary>
	/// <param name="recursive">Whether the children include the children of children and so on.</param>
	/// <returns>An <see cref="IEnumerable{T}"/> that enables this enumeration.</returns>
	public IEnumerable<Entity> LoopChildren(bool recursive = false)
	{
		if (children.Count == 0) yield break;

		foreach (Entity directChild in children)
		{
			yield return directChild;
			if (!recursive) continue;

			foreach (Entity child in directChild.children) yield return child;
		}
	}

	void RecalculateTransform()
	{
		if (!transformDirty) return;

		Float4x4 transform = Float4x4.Transformation(Position, Rotation, (Float3)Scale);

		if (Parent == null)
		{
			_forwardTransform = transform;
			_inverseTransform = transform.Inversed;
		}
		else
		{
			Parent.RecalculateTransform();

			_forwardTransform = Parent.ForwardTransform * transform;
			_inverseTransform = Parent.InverseTransform * transform.Inversed;
		}

		transformDirty = false;
	}

	void DirtyTransform()
	{
		if (transformDirty) return;
		transformDirty = true;

		foreach (Entity child in children) child.DirtyTransform();
	}
}