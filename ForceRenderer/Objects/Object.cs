using System;
using System.Collections.Generic;
using CodeHelpers;
using CodeHelpers.Vectors;
using ForceRenderer.CodeHelpers.Vectors;

namespace ForceRenderer.Objects
{
	public class Object
	{
		public Object() => children = new Children(this);

		Float3 _position;
		Float3 _rotation;
		Float3 _scale = Float3.one;

		public Float3 Position
		{
			get => _position;
			set
			{
				if (_position == value) return;
				_position = value;

				RecalculateTransformations();
				OnTransformationChanged?.Invoke();
			}
		}

		public Float3 Rotation
		{
			get => _rotation;
			set
			{
				if (_rotation == value) return;
				_rotation = value;

				RecalculateTransformations();
				OnTransformationChanged?.Invoke();
			}
		}

		public Float3 Scale
		{
			get => _scale;
			set
			{
				if (_scale == value) return;
				_scale = value;

				RecalculateTransformations();
				OnTransformationChanged?.Invoke();
			}
		}

		public Float4x4 LocalToWorld { get; private set; } = Float4x4.identity;
		public Float4x4 WorldToLocal { get; private set; } = Float4x4.identity;

		public event Action OnTransformationChanged;

		//TODO: Currently child/parent relationship does not affect transformation
		public readonly Children children;

		public Object Parent { get; private set; }
		public int ParentIndex { get; private set; }

		void RecalculateTransformations()
		{
			LocalToWorld = Float4x4.Transformation(Position, Rotation, Scale);
			WorldToLocal = LocalToWorld.Inversed;
		}

		public class Children
		{
			public Children(Object source) => this.source = source;

			readonly Object source;
			readonly List<Object> children = new List<Object>();

			public int Count => children.Count;

			public Object this[int index]
			{
				get => children[index];
				set
				{
					Object child = children[index];
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

			public void Add(Object child)
			{
				if (child == null) throw ExceptionHelper.Invalid(nameof(child), InvalidType.isNull);
				if (children.Contains(child)) throw ExceptionHelper.Invalid(nameof(child), child, "already present!");

				children.Add(child);
				ConnectChild(child, children.Count - 1);
			}

			public void RemoveAt(int index)
			{
				Object child = children[index];
				children.RemoveAt(index);

				DisconnectChild(child);
			}

			void ConnectChild(Object child, int index)
			{
				child.ParentIndex = index;
				child.Parent = source;

				child.RecalculateTransformations();
			}

			static void DisconnectChild(Object child)
			{
				child.ParentIndex = 0;
				child.Parent = null;

				child.RecalculateTransformations();
			}
		}

	}
}