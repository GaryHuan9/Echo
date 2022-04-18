using CodeHelpers.Packed;
using SFML.Graphics;

namespace Echo.UserInterface.Core.Areas;

public class ImageUI : AreaUI
{
	bool _keepAspect = true;
	Texture _texture;

	public bool KeepAspect
	{
		get => _keepAspect;
		set
		{
			if (_keepAspect == value) return;

			_keepAspect = value;
			transform.MarkDirty();
		}
	}

	public Texture Texture
	{
		get => _texture;
		set
		{
			if (_texture == value) return;
			_texture = value;

			transform.MarkDirty();
			display.Texture = value;

			Int2 size = value.Size.Cast();
			display.TextureRect = new IntRect(0, 0, size.X, size.Y);
		}
	}

	public Color ImageColor
	{
		get => display.FillColor;
		set => display.FillColor = value;
	}

	readonly RectangleShape display = new RectangleShape();

	protected override void Reorient(Float2 position, Float2 dimension)
	{
		base.Reorient(position, dimension);
		if (Texture == null) return;

		if (KeepAspect)
		{
			float aspect = (float)Texture.Size.X / Texture.Size.Y;
			int majorAxis = aspect * dimension.Y / dimension.X > 1f ? 0 : 1;

			Float2 center = position + dimension / 2f;
			float border = dimension[majorAxis];

			dimension = new Float2(border * aspect, border);
			if (majorAxis == 0) dimension /= aspect;

			position = center - dimension / 2f;
		}

		display.Position = position.As();
		display.Size = dimension.As();
	}

	protected override void Paint(Float2 min, Float2 max)
	{
		base.Paint(min, max);
		if (Texture != null) Root.PaintTexture(display, min, max);
	}
}