using Echo.Core.Compute;
using Echo.Terminal.Core;

namespace Echo.Terminal.Interface.Report;

public class DeviceReportUI : AreaUI
{
	public Device Device { get; set; }

	protected override void Draw(in Domain.Drawer drawer)
	{
		Device device = Device;

		if (device == null)
		{
			// Domain
		}
		else { }
	}
}