using CodeHelpers.Mathematics;

namespace EchoRenderer.UserInterface.Core.Interactions;

public interface IHoverable
{
	bool Hoverable => true;

	void OnMouseHovered(MouseHover mouse) { }
	void OnMousePressed(MousePress mouse) { }
	void OnMouseScrolled(Float2 delta) { }
}