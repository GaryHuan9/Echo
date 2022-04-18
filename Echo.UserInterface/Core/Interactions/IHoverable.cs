using CodeHelpers.Packed;

namespace Echo.UserInterface.Core.Interactions;

public interface IHoverable
{
	bool Hoverable => true;

	void OnMouseHovered(MouseHover mouse) { }
	void OnMousePressed(MousePress mouse) { }
	void OnMouseScrolled(Float2 delta) { }
}