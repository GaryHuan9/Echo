using Echo.Terminal.Core;

namespace Echo.Terminal.Interface;

public class ControlUI : AreaUI
{
	public override void Update()
	{
		Domain.Fill();
		Domain.WriteLine(0, ">> ");
	}
}