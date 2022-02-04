using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.UserInterface.Core.Interactions;
using SFML.Graphics;
using SFML.Graphics.Glsl;
using SFML.System;
using SFML.Window;

namespace EchoRenderer.UserInterface.Core.Areas;

public class RootUI : AreaUI, IHoverable
{
	public RootUI(Application application)
	{
		this.application = application;

		application.Resized += OnResize;

		application.MouseMoved += OnMouseMoved;
		application.MouseButtonPressed += OnMouseButtonPressed;
		application.MouseButtonReleased += OnMouseButtonReleased;

		application.MouseWheelScrolled += OnMouseWheelScrolled;
	}

	public readonly Application application;

	public IHoverable MouseHovering { get; private set; }
	public IHoverable MousePressing { get; private set; }

	readonly Shader borderRegularShader = Shader.FromString(null, null, BorderRegularShader);
	readonly Shader borderTextureShader = Shader.FromString(null, null, BorderTextureShader);

	readonly Dictionary<Type, AreaUI> typeMapper = new Dictionary<Type, AreaUI>();

	const string BorderShader = @"

uniform vec2 resolution;
uniform vec4 border;

float GetClipping()
{
	vec2 point = gl_FragCoord.xy;
	point.y = resolution.y - point.y;

	vec2 clip = step(border.xy, point) - step(border.zw, point);

	return clip.x * clip.y;
}

";

	const string BorderRegularShader = BorderShader + @"

void main()
{
	gl_FragColor = gl_Color * GetClipping();
}

";

	const string BorderTextureShader = BorderShader + @"

uniform sampler2D texture;

void main()
{
    // lookup the pixel in the texture
    vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);
	gl_FragColor = gl_Color * pixel * GetClipping();
}

";

	public void Resize(Float2 size)
	{
		Reorient(Float2.zero, size);
		transform.MarkDirty();
	}

	public T Find<T>() where T : AreaUI => (T)Find(typeof(T));

	public AreaUI Find(Type type)
	{
		if (typeMapper.TryGetValue(type, out AreaUI value))
		{
			if (value.Root == this) return value;
			typeMapper.Remove(type);
		}

		AreaUI result = Search(this);
		if (result == null) return null;

		typeMapper[type] = result;
		return result;

		AreaUI Search(AreaUI current)
		{
			if (type.IsInstanceOfType(current)) return current;
			foreach (AreaUI child in current!.LoopForward(false))
			{
				AreaUI search = Search(child);
				if (search == null) continue;

				return search;
			}

			return null;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PaintRegular(Drawable drawable, Float2 min, Float2 max)
	{
		borderRegularShader.SetUniform("border", new Vec4(min.x, min.y, max.x, max.y));
		drawable.Draw(application, new RenderStates(borderRegularShader));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PaintTexture(Drawable drawable, Float2 min, Float2 max)
	{
		borderTextureShader.SetUniform("border", new Vec4(min.x, min.y, max.x, max.y));
		borderTextureShader.SetUniform("texture", Shader.CurrentTexture);
		drawable.Draw(application, new RenderStates(borderTextureShader));
	}

	protected override void Reorient(Float2 position, Float2 dimension)
	{
		base.Reorient(position, dimension);
		Vec2 resolution = dimension.As();

		borderRegularShader.SetUniform("resolution", resolution);
		borderTextureShader.SetUniform("resolution", resolution);
	}

	void OnResize(object sender, SizeEventArgs args)
	{
		var size = new Vector2f(args.Width, args.Height);
		application.SetView(new View(size / 2f, size));

		Resize(size.As());
	}

	void OnMouseMoved(object sender, MouseMoveEventArgs args)
	{
		IHoverable touching = Find(args.As());

		if (MouseHovering != touching)
		{
			touching?.OnMouseHovered(new MouseHover(args, MouseHover.Type.enter));
			MouseHovering?.OnMouseHovered(new MouseHover(args, MouseHover.Type.exit));

			MouseHovering = touching;
		}
		else MouseHovering?.OnMouseHovered(new MouseHover(args, MouseHover.Type.roam));
	}

	void OnMouseButtonPressed(object sender, MouseButtonEventArgs args)
	{
		if (MouseHovering == null) return;
		MousePressing = MouseHovering;

		MousePressing.OnMousePressed(new MousePress(args, MousePress.Type.down));
	}

	void OnMouseButtonReleased(object sender, MouseButtonEventArgs args)
	{
		MousePressing?.OnMousePressed(new MousePress(args, MousePress.Type.up));
		MousePressing = null;
	}

	void OnMouseWheelScrolled(object sender, MouseWheelScrollEventArgs args)
	{
		int axis = args.Wheel switch
		{
			Mouse.Wheel.HorizontalWheel => 0,
			Mouse.Wheel.VerticalWheel => 1,
			_ => throw ExceptionHelper.Invalid(nameof(args.Wheel), args.Wheel, InvalidType.unexpected)
		};

		MouseHovering?.OnMouseScrolled(Float2.Create(axis, args.Delta));
	}
}