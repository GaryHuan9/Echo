using Echo.Common.Compute;
using Echo.Terminal.Core.Display;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal.Application.Report;

public abstract class ReportTI : AreaTI
{
	protected override void Paint(in Canvas canvas)
	{
		var brush = new Brush();
		var device = Device.Instance;

		if (device == null)
		{
			canvas.WriteLine(ref brush, "No compute device found.");
			canvas.WriteLine(ref brush, "Attach a device to begin.");
		}
		else Paint(canvas, brush, device);
	}

	protected abstract void Paint(in Canvas canvas, Brush brush, Device device);
}