using Echo.Terminal.Application;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal;

public class EchoTI : RootTI
{
	public EchoTI()
	{
		// Child0 = new CommandTI();
		Child1 = new ParentTI { Horizontal = true, Child0 = new CommandTI() };

		Division = 0.15f;
	}
}