using Echo.Terminal.Core;
using Echo.Terminal.Interface;

namespace Echo.Terminal;

public class EchoTerminalUI : RootUI
{
	public EchoTerminalUI()
	{
		Child0 = new ParentUI { Horizontal = true };
		Child1 = new ControlUI();

		Division = 0.85f;
	}
}