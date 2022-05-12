using Echo.Terminal.Application;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal;

public class EchoTI : RootTI
{
	public EchoTI()
	{
		// Horizontal = true;
		// Child0 = new CommandTI();
		Child1 = new BisectionTI() {Horizontal = true, Child0 = new CommandTI() };
		
		Balance = 0.15f;
	}
}