using Echo.Core.Compute;
using Echo.Terminal.Core.Display;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal.Application.Report;

public class DeviceReportTI : AreaTI
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