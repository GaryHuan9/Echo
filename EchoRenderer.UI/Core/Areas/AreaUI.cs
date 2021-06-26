using System;
using System.Collections.Generic;
using CodeHelpers.Mathematics;
using EchoRenderer.UI.Core.Interactions;
using SFML.Graphics;

namespace EchoRenderer.UI.Core.Areas
{
	public partial class AreaUI : IDisposable
	{
		public AreaUI()
		{
			transform = new Transform(this);
			panel.FillColor = Color.Transparent;

			if (this is RootUI root) Root = root;

			// Random random = RandomHelper.CurrentRandom;
			// Span<byte> bytes = stackalloc byte[3];
			//
			// random.NextBytes(bytes);
			//
			// panel.FillColor = new Color(bytes[0], bytes[1], bytes[2]);
		}

		public readonly Transform transform;
		public AreaUI Parent { get; private set; }

		RootUI _root;

		public RootUI Root
		{
			get => _root;
			private set
			{
				if (_root == value) return;

				var old = _root;
				_root = value;

				foreach (AreaUI child in LoopForward(false)) child.Root = value;

				OnRootChanged(old);
			}
		}

		bool _visible = true;
		bool _enabled = true;

		/// <summary>
		/// Whether the <see cref="AreaUI"/> is visible or not. If true, the object is drawn, receives mouse events and <see cref="Update"/> invocations.
		/// If false, the object is invisible and do not receive mouse events. However <see cref="Update"/> is still invoked if <see cref="Enabled"/> is true
		/// </summary>
		public bool Visible
		{
			get => _visible && Enabled;
			set => _visible = value;
		}

		/// <summary>
		/// If this property is toggled to false, no event nor method is raised; the <see cref="AreaUI"/> becomes a ghost.
		/// There is little difference between using the <see cref="Remove"/> method apart from the performance overhead.
		/// </summary>
		public bool Enabled
		{
			get => _enabled;
			set
			{
				if (_enabled == value) return;
				_enabled = value;

				if (Parent == null) return;
				Parent.ChildCount += value ? 1 : -1;
			}
		}

		public Color PanelColor
		{
			get => panel.FillColor;
			set => panel.FillColor = value;
		}

		readonly RectangleShape panel = new RectangleShape();
		readonly List<AreaUI> children = new List<AreaUI>();

		protected Float2 Position => panel.Position.As();
		protected Float2 Dimension => panel.Size.As();

		public int ChildCount { get; private set; }
		protected static Theme Theme => Theme.Current;

		public AreaUI Add(AreaUI child)
		{
			if (Contains(child) || HasAncestor(child) || child is RootUI) return this;

			child.Parent?.Remove(child);
			child.transform.MarkDirty();

			children.Add(child);
			child.Parent = this;
			child.Root = Root;

			if (child.Enabled) ++ChildCount;

			return this;
		}

		public bool Remove(AreaUI child)
		{
			if (!children.Remove(child)) return false;

			child.Parent = null;
			child.Root = null;

			if (child.Enabled) --ChildCount;

			return true;
		}

		public bool Contains(AreaUI child) => child.Parent == this;

		public virtual void Update()
		{
			foreach (AreaUI child in LoopForward()) child.Update();
		}

		public void Draw(RenderTarget renderTarget, bool paint = true)
		{
			transform.Reorient();

			Float2 min = default;
			Float2 max = default;

			if (paint)
			{
				GetMinMax(this, out min, out max);

				paint &= Visible && max > min;
				if (paint) Paint(renderTarget);
			}

			foreach (AreaUI child in LoopForward())
			{
				bool paintChild = false;

				if (paint)
				{
					GetMinMax(child, out Float2 childMin, out Float2 childMax);
					paintChild = childMin < max && min < childMax;
				}

				child.Draw(renderTarget, paintChild);
			}
		}

		/// <summary>
		/// Invoked on <see cref="Root"/> when the <see cref="Application"/> terminates.
		/// </summary>
		public virtual void Dispose()
		{
			foreach (AreaUI child in LoopForward(false)) child.Dispose();

			GC.SuppressFinalize(this);
		}

		protected virtual void Reorient(Float2 position, Float2 dimension)
		{
			panel.Position = position.As();
			panel.Size = dimension.As();
		}

		protected virtual void Paint(RenderTarget renderTarget)
		{
			if (PanelColor.A > 0) renderTarget.Draw(panel);
		}

		protected virtual void OnRootChanged(RootUI previous) { }

		protected IHoverable Find(Float2 point)
		{
			GetMinMax(this, out Float2 min, out Float2 max);

			if (min <= point && point <= max)
			{
				//Must iterate in reverse so that the layering is correct

				foreach (AreaUI child in LoopBackward())
				{
					var found = child.Find(point);
					if (found != null) return found;
				}

				return Visible && this is IHoverable {Hoverable: true} touchable ? touchable : null;
			}

			return null;
		}

		bool HasAncestor(AreaUI ancestor)
		{
			AreaUI current = Parent;

			while (current != null)
			{
				if (current == ancestor) return true;
				current = current.Parent;
			}

			return false;
		}

		static void GetMinMax(AreaUI target, out Float2 min, out Float2 max)
		{
			min = target.panel.Position.As();
			max = min + target.panel.Size.As();
		}
	}
}