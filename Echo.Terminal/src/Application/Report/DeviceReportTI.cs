using System;
using Echo.Common.Compute;
using Echo.Terminal.Core.Display;

namespace Echo.Terminal.Application.Report;

public class DeviceReportTI : ReportTI
{
	protected override void Paint(in Canvas canvas, Brush brush, Device device)
	{
		//Write device status
		ReadOnlySpan<IWorker> workers = device.Workers;

		canvas.WriteLine(ref brush, $"CPU compute device {(device.IsIdle ? "idle" : "running")}");
		canvas.WriteLine(ref brush, $"Population {device.Population}");
		canvas.WriteLine(ref brush, $"Progress {device.StartedProgress:P2}");
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