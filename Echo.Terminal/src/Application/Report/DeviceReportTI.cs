using System.Collections.Immutable;
using Echo.Core.Common.Compute;
using Echo.Terminal.Core.Display;

namespace Echo.Terminal.Application.Report;

public class DeviceReportTI : ReportTI
{
	protected override void Paint(in Canvas canvas, Brush brush, Device device)
	{
		//Write device status
		ImmutableArray<IWorker> workers = device.Workers;

		canvas.WriteLine(ref brush, $"CPU compute device {(device.IsDispatched ? "running" : "idle")}");
		canvas.WriteLine(ref brush, $"Population {device.Population}");
		canvas.FillLine(ref brush);

		//Write worker status
		foreach (IWorker worker in workers)
		{
			string label = worker.DisplayLabel;
			string state = worker.State.ToDisplayString();

			canvas.WriteLine(ref brush, $"{label} {state}");
		}

		//Fill remaining space
		canvas.FillAll(ref brush);
	}
}