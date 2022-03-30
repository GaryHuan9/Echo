using System;
using System.Collections.Generic;
using System.Linq;
using CodeHelpers;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using CodeHelpers.Pooling;

namespace EchoRenderer.Core.Scenic;

public class Entity
{
	public Entity() => children = new Children(this);

	Float3 _position;
	Float3 _rotation;
	Float3 _scale = Float3.One;

	public virtual Float3 Position
	{
		get => _position;
		set
		{
			if (_position == value) return;
			_position = value;

			RecalculateTransformations();
			OnTransformationChangedMethods?.Invoke();
		}
	}

	public virtual Float3 Rotation
	{
		get => _rotation;
		set
		{
			if (_rotation == value) return;
			_rotation = value;

			RecalculateTransformations();
			OnTransformationChangedMethods?.Invoke();
		}
	}

	public virtual Float3 Scale
	{
		get => _scale;
		set
		{
			if (_scale == value) return;
			_scale = value;

			RecalculateTransformations();
			OnTransformationChangedMethods?.Invoke();
		}
	}

	public Float4x4 LocalToWorld { get; private set; } = Float4x4.identity;
	public Float4x4 WorldToLocal { get; private set; } = Float4x4.identity;

	public event Action OnTransformationChangedMethods;

	public virtual string Name => GetType().Name;

	//TODO: Currently child/parent relationship does not affect transformation
	public readonly Children children;

	public Entity Parent { get; private set; }
	public int ParentIndex { get; private set; }

	public IEnumerable<Entity> LoopChildren(bool all)
	{
		if (children.Count == 0) yield break;

		Queue<Entity> frontier = CollectionPooler<Entity>.queue.GetObject();
		for (int i = 0; i < children.Count; i++) frontier.Enqueue(children[i]);

		while (frontier.Count > 0)
		{
			Entity child = frontier.Dequeue();

			yield return child;
			if (!all) continue;

			for (int i = 0; i < child.children.Count; i++) frontier.Enqueue(child.children[i]);
		}

		CollectionPooler<Entity>.queue.ReleaseObject(frontier);
	}

	void RecalculateTransformations()
	{
		LocalToWorld = Float4x4.Transformation(Position, Rotation, Scale);
		WorldToLocal = LocalToWorld.Inversed;
	}

	public class Children
	{
		public Children(Entity source) => this.source = source;

		readonly Entity source;
		readonly List<Entity> children = new();

		public int Count => children.Count;

		public Entity this[int index]
		{
			get => children[index];
			set
			{
				Entity child = children[index];
				if (child == value) return;

				DisconnectChild(child);

				if (value == null)
				{
					children.RemoveAt(child.ParentIndex);
					return;
				}

				children[index] = value;
				ConnectChild(value, index);
			}
		}

		public void Add(Entity child)
		{
			if (child == null) throw ExceptionHelper.Invalid(nameof(child), InvalidType.isNull);
			if (children.Contains(child)) throw ExceptionHelper.Invalid(nameof(child), child, "already present!");

			children.Add(child);
			ConnectChild(child, children.Count - 1);
		}

		public void RemoveAt(int index)
		{
			Entity child = children[index];
			children.RemoveAt(index);

			DisconnectChild(child);
		}

		public T FindFirst<T>() where T : Entity => (T)children.FirstOrDefault(target => target is T);

		void ConnectChild(Entity child, int index)
		{
			child.ParentIndex = index;
			child.Parent = source;

			child.RecalculateTransformations();
		}

		static void DisconnectChild(Entity child)
		{
			child.ParentIndex = 0;
			child.Parent = null;

			child.RecalculateTransformations();
		}
	}

}