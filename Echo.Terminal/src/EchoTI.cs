using Echo.Terminal.Application;
using Echo.Terminal.Core;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal;

public class EchoTI : RootTI
{
	public EchoTI()
	{
		Child0 = new ParentTI { Horizontal = true };
		Child1 = new CommandTI();

		Division = 0.85f;
	}
}