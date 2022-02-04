using CodeHelpers.Mathematics;
using SFML.Window;

namespace EchoRenderer.UserInterface.Core.Interactions;

public readonly struct MouseHover
{
	public MouseHover(MouseMoveEventArgs args, Type type)
	{
		point = args.As();
		this.type = type;
	}

	public readonly Float2 point;
	public readonly Type type;

	public enum Type : byte
	{
		enter,
		exit,
		roam
	}
}

public readonly struct MousePress
{
	public MousePress(MouseButtonEventArgs args, Type type)
	{
		button = args.Button;
		point = args.As();
		this.type = type;
	}

	public readonly Mouse.Button button;
	public readonly Float2 point;
	public readonly Type type;

	public enum Type : byte
	{
		down,
		up
	}
}