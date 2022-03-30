using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using SFML.Graphics;

namespace EchoRenderer.UserInterface.Core.Areas;

public partial class AreaUI
{
	public class Transform
	{
		public Transform(AreaUI area) => this.area = area;

		readonly AreaUI area;

		float _rightPercent;
		float _bottomPercent;
		float _leftPercent;
		float _topPercent;

		public float RightPercent
		{
			get => _rightPercent;
			set => Assign(ref _rightPercent, value);
		}

		public float BottomPercent
		{
			get => _bottomPercent;
			set => Assign(ref _bottomPercent, value);
		}

		public float LeftPercent
		{
			get => _leftPercent;
			set => Assign(ref _leftPercent, value);
		}

		public float TopPercent
		{
			get => _topPercent;
			set => Assign(ref _topPercent, value);
		}

		float _rightMargin;
		float _bottomMargin;
		float _leftMargin;
		float _topMargin;

		public float RightMargin
		{
			get => _rightMargin;
			set => Assign(ref _rightMargin, value);
		}

		public float BottomMargin
		{
			get => _bottomMargin;
			set => Assign(ref _bottomMargin, value);
		}

		public float LeftMargin
		{
			get => _leftMargin;
			set => Assign(ref _leftMargin, value);
		}

		public float TopMargin
		{
			get => _topMargin;
			set => Assign(ref _topMargin, value);
		}

		public float VerticalPercents
		{
			get => (BottomPercent + TopPercent) / 2f;
			set => BottomPercent = TopPercent = value;
		}

		public float VerticalMargins
		{
			get => (BottomMargin + TopMargin) / 2f;
			set => BottomMargin = TopMargin = value;
		}

		public float HorizontalPercents
		{
			get => (RightPercent + LeftPercent) / 2f;
			set => RightPercent = LeftPercent = value;
		}

		public float HorizontalMargins
		{
			get => (RightMargin + LeftMargin) / 2f;
			set => RightMargin = LeftMargin = value;
		}

		public float UniformPercents
		{
			get => (RightPercent + BottomPercent + LeftPercent + TopPercent) / 4f;
			set => RightPercent = BottomPercent = LeftPercent = TopPercent = value;
		}

		public float UniformMargins
		{
			get => (RightMargin + BottomMargin + LeftMargin + TopMargin) / 4f;
			set => RightMargin = BottomMargin = LeftMargin = TopMargin = value;
		}

		float _preferedWidth = -1f;
		float _preferedHeight = -1f;

		public float? PreferedWidth //Currently unused, will have similar functionally as PreferedHeight
		{
			get => _preferedWidth < 0f ? null : _preferedWidth;
			set => Assign(ref _preferedWidth, value ?? -1f, area.Parent.transform);
		}

		public float? PreferedHeight //Used by auto layout components to calculate positioning
		{
			get => _preferedHeight < 0f ? null : _preferedHeight;
			set => Assign(ref _preferedHeight, value ?? -1f, area.Parent.transform);
		}

		/// <summary>
		/// The value of the latest frame this transform has been dirtied.
		/// NOTE: A value of zero means the transform is not dirtied.
		/// </summary>
		ulong frameDirtied;

		public bool Dirtied => frameDirtied != 0;

		/// <summary>
		/// The current frame. Value change be changed by <see cref="IncrementFrame"/>.
		/// NOTE: A value of zero is reserved: <see cref="frameDirtied"/> for more info.
		/// </summary>
		static ulong currentFrame = 1;

		public void Reorient()
		{
			if (!Dirtied || area.Parent == null) return;
			RectangleShape parent = area.Parent.panel;

			ClearDirty();

			Float2 parentPosition = parent.Position.As();
			Float2 parentDimension = parent.Size.As();

			Float2 position = parentPosition + parentDimension * new Float2
			(
				LeftPercent,
				TopPercent
			) + new Float2
			(
				LeftMargin,
				TopMargin
			);

			Float2 dimension = parentDimension * new Float2
			(
				1f - RightPercent - LeftPercent,
				1f - TopPercent - BottomPercent
			) - new Float2
			(
				RightMargin + LeftMargin,
				TopMargin + BottomMargin
			);

			area.Reorient(position, dimension);
		}

		public void MarkDirty()
		{
			if (frameDirtied == currentFrame) return;
			frameDirtied = currentFrame;

			//Dirtying the parent dirties the entire hierarchy (excluding disabled UI)
			foreach (AreaUI child in area.LoopForward()) child.transform.MarkDirty();
		}

		public static void IncrementFrame() => ++currentFrame;

		/// <summary>
		/// Tries to clear the <see cref="Dirtied"/> mark from this <see cref="Transform"/>.
		/// NOTE: We can only clear dirtiness before our current <see cref="frameDirtied"/>.
		/// </summary>
		void ClearDirty()
		{
			if (frameDirtied < currentFrame) frameDirtied = 0;
		}

		/// <summary>
		/// Assign <paramref name="value"/> to <paramref name="original"/>. If the number is altered, we will invoke <see cref="MarkDirty"/>
		/// on <paramref name="target"/>. If <paramref name="target"/> is null, we will use this <see cref="Transform"/> as the target.
		/// </summary>
		void Assign(ref float original, float value, Transform target = null)
		{
			if (!original.AlmostEquals(value))
			{
				target ??= this;
				target.MarkDirty();
			}

			original = value;
		}
	}
}