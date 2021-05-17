using System.Collections;
using System.Collections.Generic;

namespace EchoRenderer.GUI.Core
{
	public class AreaGUI : IEnumerable<AreaGUI>
	{
		public readonly Transform position = new Transform();

		public AreaGUI Parent { get; private set; }

		readonly List<AreaGUI> children = new List<AreaGUI>();

		public AreaGUI this[int index] => children[index];
		public int ChildCount => children.Count;

		public virtual void Add(AreaGUI child)
		{
			if (Contains(child)) return;
			child.Parent?.Remove(child);

			children.Add(child);
			child.Parent = this;

			child.Reorient();
		}

		public virtual bool Remove(AreaGUI child)
		{
			if (!children.Remove(child)) return false;

			child.Parent = null;
			return true;
		}

		public bool Contains(AreaGUI child) => child.Parent == this;

		public void Reorient()
		{
			if (Parent != null)
			{

			}

			foreach (AreaGUI child in this) child.Reorient();
		}

		public List<AreaGUI>.Enumerator GetEnumerator() => children.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		IEnumerator<AreaGUI> IEnumerable<AreaGUI>.GetEnumerator() => GetEnumerator();

		public class Transform
		{
			public float RightPercent { get; set; }
			public float RightMargin { get; set; }

			public float DownPercent { get; set; }
			public float DownMargin { get; set; }

			public float LeftPercent { get; set; }
			public float LeftMargin { get; set; }

			public float UpPercent { get; set; }
			public float UpMargin { get; set; }

			public void UniformPercent(float percent)
			{
				RightPercent = DownPercent = percent;
				LeftPercent = UpPercent = percent;
			}

			public void UniformMargin(float margin)
			{
				RightMargin = DownMargin = margin;
				LeftMargin = UpMargin = margin;
			}
		}
	}
}